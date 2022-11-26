'use strict';

const transformFile = process.argv[2];
const inputFile = process.argv[3];
const outputFile = process.argv[4];
const transform = require(transformFile);
const fs = require("fs");
const path = require("path");

if (!(transform instanceof Function))
    throw "'" + path.basename(transformFile) + "' must export a single function.";

fs.readFile(inputFile, "utf8", (err, data) => {
    if (err)
        throw err;

    data = data.replace(/^\uFEFF/, '');

    let schema = JSON.parse(data);

    transform(schema);

    fs.writeFile(outputFile, JSON.stringify(schema, null, 4), "utf8", function (err) {
        if (err)
            throw err;
    });
});