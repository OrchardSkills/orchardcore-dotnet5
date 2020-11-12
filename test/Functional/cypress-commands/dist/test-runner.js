'use strict';

Object.defineProperty(exports, '__esModule', { value: true });

// This module was originally build by the OrchardCore team
const child_process = require("child_process");
const fs = require("fs-extra");
const path = require("path");

global.log = function(msg) {
  let now = new Date().toLocaleTimeString();
  console.log(`[${now}] ${msg}\n`);
};

// Build the dotnet application in release mode
function build(dir) {
  global.log("Building ...");
  child_process.spawnSync("dotnet", ["build", "-c", "Release"], { cwd: dir });
}

// destructive action that deletes the App_Data folder
function clean(dir) {
  const appdataPath = path.join(dir, "App_Data");
  fs.removeSync(appdataPath);
  global.log(`${appdataPath} deleted`);
}
// clean and copy some files 
function setup(dir, copyDir) {
    clean(dir);
    const appdataPath = path.join(dir, "App_Data");
    fs.ensureDirSync(appdataPath);
    fs.copySync(copyDir, appdataPath);
    global.log(`Copied SaaS tenant to ${appdataPath}`);
}

// Host the dotnet application, does not rebuild
function host(dir, assembly) {
  if (fs.existsSync(path.join(dir, "bin/Release/net5.0/", assembly))) {
    global.log("Application already built, skipping build");
  } else {
    build(dir);
  }
  global.log("Starting application ..."); 
  
  const ocEnv = {};

  Object.keys(process.env).forEach((v) => {
    if(v.startsWith('OrchardCore__')){
        ocEnv[v] = process.env[v];
    }
  });
  console.log(`Environment variables:`, ocEnv);
  let server = child_process.spawn(
    "dotnet",
    ["bin/Release/net5.0/" + assembly],
    { cwd: dir, env: process.env }
  );

  server.stdout.on("data", data => {
    global.log(data);
  });

  server.stderr.on("data", data => {
    global.log(`stderr: ${data}`);
  });

  server.on("close", code => {
    global.log(`Server process exited with code ${code}`);
  });
  return server;
}

// combines the functions above, useful when triggering tests from CI
function e2e(dir, assembly) {

  var server = host(dir, assembly);

  let test = child_process.exec("npx cypress run");
  test.stdout.on("data", data => {
    console.log(data);
  });

  test.stderr.on("data", data => {
    console.log(`stderr: ${data}`);
  });

  test.on("close", code => {
    console.log(`Cypress process exited with code ${code}`);
    server.kill("SIGINT");
    process.exit(code);
  });
}

exports.build = build;
exports.clean = clean;
exports.e2e = e2e;
exports.host = host;
exports.setup = setup;
