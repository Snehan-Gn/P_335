const swaggerAutogen = require("swagger-autogen")();

const doc = {
  info: {
    title: "PWeb API",
    description: "Swagger documentation pour PWeb API",
  },
  host: "localhost:3000",
  schemes: ["http"],
};

const outputFile = "./swagger-output.json";
const routes = ["./app.js"];

swaggerAutogen(outputFile, routes, doc);
