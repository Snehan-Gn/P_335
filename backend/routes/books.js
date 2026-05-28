const { User, Book, Comment, Category } = require("../models");
const { Op } = require("sequelize");
const fs = require("fs");
const path = require("path");

var express = require("express");
var router = express.Router();
var EPub = require("epub").EPub;
var multer = require("multer");

const booksDir = path.join(__dirname, "../public/books");
if (!fs.existsSync(booksDir)) fs.mkdirSync(booksDir, { recursive: true });

const storage = multer.diskStorage({
  destination: function (req, file, cb) {
    cb(null, booksDir);
  },
  filename: function (req, file, cb) {
    const uniqueSuffix = Date.now() + "-" + Math.round(Math.random() * 1e9);
    cb(null, file.fieldname + "-" + uniqueSuffix + ".epub");
  },
});

var upload = multer({ storage: storage });

async function getMissingData(title, author) {
  try {
    let query = `intitle:${encodeURIComponent(title)}`;
    if (author) query += `+inauthor:${encodeURIComponent(author)}`;
    const url = `https://www.googleapis.com/books/v1/volumes?q=${query}`;
    const response = await fetch(url);
    const data = await response.json();
    if (data.error && data.error.code == 429) {
      return { title, author, description: null, publish_date: null, language_: null, isbn: null, cover_image_url: null };
    }
    if (data.error && data.error.code == 503) {
      await new Promise((resolve) => setTimeout(resolve, 2000));
      return getMissingData(title, author);
    }
    if (!data.items || data.items.length === 0) return null;
    const volumeInfo = data.items[0].volumeInfo;
    return {
      title: volumeInfo.title || title,
      author: volumeInfo.authors ? volumeInfo.authors[0] : null,
      description: volumeInfo.description || null,
      publish_date: volumeInfo.publishedDate || null,
      language_: volumeInfo.language || null,
      isbn: volumeInfo.industryIdentifiers ? volumeInfo.industryIdentifiers[0].identifier : null,
      cover_image_url: volumeInfo.imageLinks ? volumeInfo.imageLinks.thumbnail : null,
    };
  } catch {
    return null;
  }
}

function parseEpub(filePath) {
  const epub = new EPub(filePath);
  // This version of `epub` doesn't expose EventEmitter-style `.on("end")`.
  // `parse()` returns a Promise.
  return epub.parse().then(() => epub);
}

router.get("/", async function (req, res, next) {
  try {
    const { title, author, category, limit, sort } = req.query;
    const user = await User.findOne({ where: { user_id: req.user.user_id } });

    const where = {};

    if (title) where.title = { [Op.substring]: title };
    if (author) where.author = { [Op.substring]: author };

    const include = [
      { model: Comment },
      {
        model: User,
        where: { user_id: req.user.user_id },
        attributes: [],
        through: { attributes: [] },
      },
    ];

    if (category) {
      include.push({
        model: Category,
        where: { name: { [Op.substring]: category } },
        through: { attributes: [] },
      });
    }

    let order = [["book_id", "ASC"]];
    if (sort) {
      const direction = sort.toUpperCase();
      if (direction === "ASC" || direction === "DESC") {
        order = [["book_id", direction]];
      } else {
        order = [[sort, "ASC"]];
      }
    }

    const books = await user.getBooks({
      where,
      include,
      limit: limit ? parseInt(limit) : undefined,
      distinct: true,
      order,
    });

    res.json(books);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

router.get("/:id", async function (req, res, next) {
  try {
    const user = await User.findOne({ where: { user_id: req.user.user_id } });
    const book = await user.getBooks({
      where: { book_id: req.params.id },
      include: [{ model: Comment }],
    });
    if (!book) return res.status(404).json({ error: "Book not found" });
    return res.json(book);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

router.post("/", upload.any(), async function (req, res, next) {
  const uploadedFile =
    req.file ||
    (Array.isArray(req.files) && req.files.length > 0 ? req.files[0] : null);

  if (!uploadedFile) {
    return res.status(400).json({
      error:
        "Aucun fichier n'a été téléchargé. Attendu: multipart/form-data avec un fichier (ex: champ epub_file).",
    });
  }

  try {
    const epub = await parseEpub(uploadedFile.path);
    const metadata = epub.metadata;

    const fallbackMetadata = metadata.creator
      ? await getMissingData(metadata.title, metadata.creator)
      : await getMissingData(metadata.title);

    const newBook = await Book.create({
      title: metadata.title || uploadedFile.originalname,
      author: metadata.creator || (fallbackMetadata ? fallbackMetadata.author : "Auteur inconnu"),
      description: metadata.description || (fallbackMetadata ? fallbackMetadata.description : ""),
      publish_date: metadata.date || (fallbackMetadata ? fallbackMetadata.publish_date : null),
      language_: metadata.language || (fallbackMetadata ? fallbackMetadata.language_ : null),
      isbn: metadata.identifier || (fallbackMetadata ? fallbackMetadata.isbn : null),
      url: "/books/" + uploadedFile.filename,
      cover_image_url: fallbackMetadata ? fallbackMetadata.cover_image_url : null,
    });

    const user = await User.findOne({ where: { user_id: req.user.user_id } });
    await user.addBook(newBook);

    return res.json(newBook);
  } catch (error) {
    res.status(500).json({ error: "Erreur lors du traitement : " + error.message });
  }
});

router.put("/:id", async function (req, res, next) {
  try {
    const user = await User.findOne({ where: { user_id: req.user.user_id } });
    const books = await user.getBooks({ where: { book_id: req.params.id } });
    if (!books) return res.status(404).json({ error: "Book not found" });
    const { title, author, description, publish_date, language_, isbn } = req.body;
    const book = books[0];
    await book.update({ title, author, description, publish_date, language_, isbn });
    return res.json(book);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

router.delete("/:id", async function (req, res, next) {
  try {
    const user = await User.findOne({ where: { user_id: req.user.user_id } });
    const books = await user.getBooks({ where: { book_id: req.params.id } });
    if (!books) return res.status(404).json({ error: "Book not found" });
    const book = books[0];
    await user.removeBook(book);
    return res.json({ message: "Book deleted successfully" });
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

module.exports = router;
