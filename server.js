require("dotenv").config();
const cors = require("cors");
const fileupload = require("express-fileupload");
const bodyParser = require("body-parser");
const express = require("express");
const app = express();
const mongoose = require("mongoose");

mongoose.connect(process.env.DATABASE_URL, { useNewUrlParser: true });
const db = mongoose.connection;
db.on("error", (error) => console.error(error));
db.once("open", () => console.log("Connected to Database"));
app.use(express.json());
app.use(bodyParser.json());
app.use(bodyParser.urlencoded({ extended: true }));
const contactRouter = require("./src/routes/api");
app.use(
  cors({
    origin: "*"
  })
);
app.use(fileupload());
app.use(express.static("files"));
app.use("/api", contactRouter);

const PORT = process.env.PORT || 5000;
app.listen(PORT, () => console.log("Server Started"));
