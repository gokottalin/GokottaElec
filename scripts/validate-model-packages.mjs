import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, "..");
const coreLibraryPath = path.resolve(repoRoot, "components/core-components.v0.1.json");
const modelLibraryPath = path.resolve(repoRoot, "components/model-packages.v0.1.json");

const coreLibrary = JSON.parse(fs.readFileSync(coreLibraryPath, "utf8"));
const modelLibrary = JSON.parse(fs.readFileSync(modelLibraryPath, "utf8"));

const errors = [];
const warnings = [];

const addError = (message) => errors.push({ level: "ERROR", message });
const addWarning = (message) => warnings.push({ level: "WARNING", message });

const terminalPattern = /^[A-Z][A-Z0-9_+\-]*$/;

const terminalAllowedBySpec = (spec, terminal) => {
  if (spec.terminals?.includes(terminal)) return true;
  if (spec.terminal_pattern && new RegExp(spec.terminal_pattern).test(terminal)) return true;
  return false;
};

const checkPinMap = (context, pinMap, componentSpec) => {
  if (!pinMap || typeof pinMap !== "object" || Array.isArray(pinMap)) {
    addError(`${context}.pin_map must be an object`);
    return;
  }

  const seenTerminals = new Set();
  for (const [pin, terminal] of Object.entries(pinMap)) {
    if (!String(pin).trim()) addError(`${context} contains an empty physical pin key`);
    if (typeof terminal !== "string" || !terminalPattern.test(terminal)) {
      addError(`${context}[${pin}] maps to invalid terminal ${terminal}`);
      continue;
    }
    if (!terminalAllowedBySpec(componentSpec, terminal)) {
      addError(`${context}[${pin}] maps to unknown terminal ${terminal}`);
    }
    if (seenTerminals.has(terminal)) {
      addError(`${context} maps multiple physical pins to terminal ${terminal}`);
    }
    seenTerminals.add(terminal);
  }
};

if (modelLibrary.library_version !== "0.1.0") {
  addError(`Unsupported model package library_version: ${modelLibrary.library_version}`);
}

if (!modelLibrary.models || typeof modelLibrary.models !== "object" || Array.isArray(modelLibrary.models)) {
  addError("Model package library must contain a models object");
}

const normalizedModelNames = new Set();
for (const [modelName, modelSpec] of Object.entries(modelLibrary.models ?? {})) {
  const normalizedModelName = modelName.toUpperCase();
  if (normalizedModelNames.has(normalizedModelName)) {
    addError(`Duplicate model key after normalization: ${modelName}`);
  }
  normalizedModelNames.add(normalizedModelName);

  for (const alias of modelSpec.aliases ?? []) {
    const normalizedAlias = String(alias).toUpperCase();
    if (normalizedModelNames.has(normalizedAlias)) {
      addWarning(`${modelName} alias ${alias} duplicates another model or alias`);
    }
    normalizedModelNames.add(normalizedAlias);
  }

  const componentSpec = coreLibrary.components?.[modelSpec.component_type];
  if (!componentSpec) {
    addError(`${modelName} references unknown component_type ${modelSpec.component_type}`);
    continue;
  }

  if (!modelSpec.packages || typeof modelSpec.packages !== "object" || Array.isArray(modelSpec.packages)) {
    addError(`${modelName}.packages must be an object`);
    continue;
  }

  if (modelSpec.default_package && !modelSpec.packages[modelSpec.default_package]) {
    addError(`${modelName}.default_package ${modelSpec.default_package} is not defined`);
  }

  for (const [packageName, packageSpec] of Object.entries(modelSpec.packages)) {
    const context = `${modelName}.${packageName}`;
    checkPinMap(context, packageSpec.pin_map, componentSpec);

    for (const [casePin, terminal] of Object.entries(packageSpec.case_map ?? {})) {
      if (!terminalAllowedBySpec(componentSpec, terminal)) {
        addError(`${context}.case_map[${casePin}] maps to unknown terminal ${terminal}`);
      }
    }

    for (const [index, tie] of (packageSpec.internal_ties ?? []).entries()) {
      if (!Array.isArray(tie) || tie.length < 2) {
        addError(`${context}.internal_ties[${index}] must contain at least two terminals`);
        continue;
      }
      for (const terminal of tie) {
        if (!terminalAllowedBySpec(componentSpec, terminal)) {
          addError(`${context}.internal_ties[${index}] references unknown terminal ${terminal}`);
        }
      }
    }

    for (const [unitName, unitPinMap] of Object.entries(packageSpec.unit_pin_maps ?? {})) {
      const unitComponentType = modelSpec.unit_component_type ?? (
        modelSpec.component_type === "OPAMP_DUAL_PACKAGE" ? "OPAMP_DUAL_UNIT" : modelSpec.component_type
      );
      const unitSpec = coreLibrary.components?.[unitComponentType];
      if (!unitSpec) {
        addError(`${context}.unit_pin_maps.${unitName} references unknown unit component_type ${unitComponentType}`);
        continue;
      }
      checkPinMap(`${context}.unit_pin_maps.${unitName}`, unitPinMap, unitSpec);
    }
  }
}

for (const item of [...errors, ...warnings]) {
  console.log(`${item.level}: ${item.message}`);
}

if (errors.length > 0) {
  process.exit(1);
}

console.log(`OK: ${path.relative(repoRoot, modelLibraryPath)}`);
