const { User, Book, Comment, Category } = require("../models");
const { Op } = require("sequelize");

var express = require("express");
var router = express.Router();
var EPub = require("epub").EPub;
var multer = require("multer");

// https://github.com/expressjs/multer
const storage = multer.diskStorage({
  destination: function (req, file, cb) {
    cb(null, "public/books/");
  },
  filename: function (req, file, cb) {
    const uniqueSuffix = Date.now() + "-" + Math.round(Math.random() * 1e9);
    cb(null, file.fieldname + "-" + uniqueSuffix + ".epub");
  },
});

var upload = multer({ dest: "public/books/", storage: storage });

async function getMissingData(title, author) {
  try {
    let query = `intitle:${encodeURIComponent(title)}`;
    if (author) {
      query += `+inauthor:${encodeURIComponent(author)}`;
    }
    const url = `https://www.googleapis.com/books/v1/volumes?q=${query}`;

    const response = await fetch(url);
    const data = await response.json();

    console.log(data.error);
    if (data.error && data.error.code == 429) {
      return {
        title: title || null,
        author: author || null,
        description: null,
        publish_date: null,
        language_: null,
        isbn: null,
        cover_image_url: null,
      };
    }
    if (data.error && data.error.code == 503) {
      // https://stackoverflow.com/questions/33289726/combination-of-async-function-await-settimeout
      await new Promise((resolve) => setTimeout(resolve, 2000));
      return getMissingData(title, author);
    }
    if (!data.items || data.items.length === 0) {
      return null;
    }

    const volumeInfo = data.items[0].volumeInfo;
    const bookData = {
      title: volumeInfo.title || title,
      author: volumeInfo.authors ? volumeInfo.authors[0] : null,
      description: volumeInfo.description || null,
      publish_date: volumeInfo.publishedDate || null,
      language_: volumeInfo.language || null,
      isbn: volumeInfo.industryIdentifiers
        ? volumeInfo.industryIdentifiers[0].identifier
        : null,
      cover_image_url: volumeInfo.imageLinks
        ? volumeInfo.imageLinks.thumbnail
        : null,
    };
    console.log("Données récupérées depuis Google Books API :", bookData);
    return bookData;
  } catch (error) {
    return null;
  }
}

router.get("/", async function (req, res, next) {
  try {
    const { title, author, category, limit, sort } = req.query;
    const user = await User.findOne({ where: { user_id: req.user.user_id } });

    const where = {};

    // https://sequelize.org/docs/v6/core-concepts/model-querying-basics/#operators

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

    console.log(req.user.user_id);
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
    if (!book) {
      return res.status(404).json({ error: "Book not found" });
    }
    return res.json(book);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

router.post("/", upload.single("epub_file"), async function (req, res, next) {
  if (!req.file) {
    return res.status(400).json({ error: "Aucun fichier n'a été téléchargé." });
  }

  try {
    const epub = new EPub(req.file.path);

    await epub.parse();
    console.log("Métadonnées :", epub.metadata);
    const metadata = epub.metadata;
    const fallbackMetadata = metadata.creator
      ? await getMissingData(metadata.title, metadata.creator)
      : await getMissingData(metadata.title);
    const newBook = await Book.create({
      title: metadata.title || req.file.originalname,
      author: metadata.creator || (fallbackMetadata ? fallbackMetadata.author : "Auteur inconnu"),
      description: metadata.description || (fallbackMetadata ? fallbackMetadata.description : ""),
      publish_date: metadata.date || (fallbackMetadata ? fallbackMetadata.publish_date : null),
      language_: metadata.language || (fallbackMetadata ? fallbackMetadata.language_ : null),
      isbn: metadata.identifier || (fallbackMetadata ? fallbackMetadata.isbn : null),
      url: "/books/" + req.file.filename,
      cover_image_url: fallbackMetadata ? fallbackMetadata.cover_image_url : null,
    });

    const user = await User.findOne({ where: { user_id: req.user.user_id } });

    await user.addBook(newBook);
    console.log(newBook);

    return res.json(newBook);
  } catch (error) {
    res
      .status(500)
      .json({ error: "Erreur lors du traitement : " + error.message });
  }
});

router.put("/:id", async function (req, res, next) {
  try {
    const user = await User.findOne({ where: { user_id: req.user.user_id } });
    const books = await user.getBooks({ where: { book_id: req.params.id } });
    if (!books) {
      return res.status(404).json({ error: "Book not found" });
    }
    const { title, author, description, publish_date, language_, isbn } =
      req.body;
    const book = books[0];
    await book.update({
      title,
      author,
      description,
      publish_date,
      language_,
      isbn,
    });
    return res.json(book);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

router.delete("/:id", async function (req, res, next) {
  try {
    const user = await User.findOne({ where: { user_id: req.user.user_id } });
    const books = await user.getBooks({ where: { book_id: req.params.id } });
    if (!books) {
      return res.status(404).json({ error: "Book not found" });
    }
    const book = books[0];
    await user.removeBook(book);
    return res.json({ message: "Book deleted successfully" });
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

module.exports = router;
