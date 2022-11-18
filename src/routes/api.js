const express = require("express");
const router = express.Router();
const fs = require("fs");
const contact = require("../models/contact");
const readXlsxFile = require("read-excel-file/node");
const Contact = require("../models/contact");

// Getting all
router.get("/", async (req, res) => {
  try {
    const contacts = await Contact.find();
    res.json(contacts);
  } catch (err) {
    res.status(500).json({ message: err.message });
  }
});

// Getting all
router.get("/search", async (req, res) => {
  try {
    const searchparam = req.query.search;
    const contacts = await Contact.find({
      name: { $regex: "\\b" + searchparam, $options: "i" },
    });
    res.json(contacts);
  } catch (err) {
    res.status(500).json({ message: err.message });
  }
});

// Getting One
router.get("/:id", getContact, (req, res) => {
  res.json(res.contact);
});

// Creating one
router.post("/", async (req, res) => {
  const contact = new Contact({
    name: req.body.name,
    phone: req.body.phone,
  });
  try {
    const newContact = await contact.save();
    res.status(201).json(newContact);
  } catch (err) {
    res.status(400).json({ message: err.message });
  }
});

// router.post("/upload", async (req, res) => {
//   try {
//     const newpath = __dirname + "/files/";
//     const file = req.files.file;
//     const filename = file.name;

//     file.mv(`${newpath}${filename}`, (err) => {
//       if (err) {
//         res.status(500).send({ message: "File upload failed", code: err });
//       }

//       fs.readFile(`${newpath}${filename}`, "utf8", async (err, data) => {
//         if (err) {
//           console.error(err);
//           return;
//         }
//         const contacts = [];
//         data.split(",").forEach((contact) => {
//           contacts.push({
//             name: contact.split("-")[0],
//             phone: contact.split("-")[1],
//           });
//         });

//         try {
//           const newContact = await Contact.insertMany(contacts);

//           res.status(201).send({ message: "File Uploaded", data: newContact });
//         } catch (err) {
//           res.status(400).json({ message: err.message });
//         }
//       });
//     });
//   } catch (error) {
//     res.status(400).json({ message: err.message });
//   }
// });

// Upload many contacts
router.post("/uploadBulkContacts", async (req, res) => {
  const file = req.files.file;
  const fileName = file.name;
  const newpath = __dirname + "/files/";
  const schema = {
    phone: {
      prop: "phone",
      type: String,
    },
    name: {
      prop: "name",
      type: String,
    },
  };

  file.mv(`${newpath}${fileName}`, async (err) => {
    if (err) {
      res.status(500).send({ message: "File upload failed", code: err });
    }
    try {
      const { rows } = await readXlsxFile(`${newpath}${fileName}`, {
        schema,
      });
      const newContact = await Contact.insertMany(rows, { ordered: false });
      res.status(201).send({ message: "Contacts created", data: newContact });
    } catch (error) {
      res.status(400).send({ message: error.message });
    }
  });
});

// Uplaod & Update many contacts
router.post("/updateBulkContacts", async (req, res) => {
  const file = req.files.file;
  const fileName = file.name;
  const newpath = __dirname + "/files/";
  const schema = {
    phone: {
      prop: "phone",
      type: String,
    },
    name: {
      prop: "name",
      type: String,
    },
  };
  file.mv(`${newpath}${fileName}`, async (err) => {
    if (err) {
      res.status(500).send({ message: "File upload failed", code: err });
    }
    try {
      const { rows: contacts } = await readXlsxFile(`${newpath}${fileName}`, {
        schema,
      });
      for (let x of contacts) {
        const newContact = await Contact.findOneAndUpdate(
          { phone: x.phone },
          { name: x.name }
        );
      }
      res.status(201).send({ message: "Contacts updated" });
    } catch (error) {
      res.status(400).json({ message: error.message });
    }
  });
});

// Uplaod & Delete many contacts
router.post("/deleteBulkContacts", async (req, res) => {
  const file = req.files.file;
  const fileName = file.name;
  const newpath = __dirname + "/files/";
  const schema = {
    phone: {
      prop: "phone",
      type: String,
    },
    name: {
      prop: "name",
      type: String,
    },
  };
  file.mv(`${newpath}${fileName}`, async (err) => {
    if (err) {
      res.status(500).send({ message: "File upload failed", code: err });
    }

    try {
      const { rows: contacts } = await readXlsxFile(`${newpath}${fileName}`, {
        schema,
      });
      const mappedContacts = contacts.map((x) => x.phone);

      let deletedContacts = await Contact.deleteMany({
        phone: { $in: mappedContacts },
      });
      if (deletedContacts == null) {
        return res.status(404).json({ message: "Cannot find contacts" });
      }
      res.status(201).send({ message: "Contacts deleted" });
    } catch (error) {
      console.log(error.message);
      res.status(400).json({ message: error.message });
    }
  });
});

// Updating One
router.patch("/:id", getContact, async (req, res) => {
  if (req.body.name != null) {
    res.contact.name = req.body.name;
  }
  if (req.body.phone != null) {
    res.contact.phone = req.body.phone;
  }
  try {
    const updatedContact = await res.contact.save();
    res.json(updatedContact);
  } catch (err) {
    res.status(400).json({ message: err.message });
  }
});

// Deleting One
router.delete("/:id", getContact, async (req, res) => {
  try {
    await res.contact.remove();
    res.json({ message: "Deleted Contact" });
  } catch (err) {
    res.status(500).json({ message: err.message });
  }
});

// Deleting Many
router.post("/bulkdelete", async (req, res) => {
  let deletedContacts;

  try {
    deletedContacts = await Contact.deleteMany({ _id: req.body.selectedids });
    if (deletedContacts == null) {
      return res.status(404).json({ message: "Cannot find contacts" });
    }
    res.json({ message: "Deleted Contacts" });
  } catch (err) {
    return res.status(500).json({ message: err.message });
  }
});

async function getContact(req, res, next) {
  let contact;
  try {
    contact = await Contact.findById(req.params.id);
    if (contact == null) {
      return res.status(404).json({ message: "Cannot find contact" });
    }
  } catch (err) {
    return res.status(500).json({ message: err.message });
  }

  res.contact = contact;
  next();
}

module.exports = router;
