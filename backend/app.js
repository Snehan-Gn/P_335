require("dotenv").config();
var express = require("express");
var path = require("path");
var cookieParser = require("cookie-parser");
var logger = require("morgan");

var indexRouter = require("./routes/index");
var loginRouter = require("./routes/login");
var usersRouter = require("./routes/users");
var booksRouter = require("./routes/books");
var commentsRouter = require("./routes/comments");
const auth = require("./middleware/auth");
const swaggerUi = require("swagger-ui-express");
const swaggerSpecs = require("./swagger.json");

var app = express();

app.use(logger("dev"));
app.use(express.json());
app.use(express.urlencoded({ extended: false }));
app.use(cookieParser());

app.use(express.static(path.join(__dirname, "public")));
app.use("/api-docs", swaggerUi.serve, swaggerUi.setup(swaggerSpecs));
app.use("/", indexRouter);
app.use("/", loginRouter);

app.use("/users", auth, usersRouter);
app.use("/comments", auth, commentsRouter);
app.use("/books", auth, booksRouter);
app.use("/categories", auth, require("./routes/categories"));

module.exports = app;
