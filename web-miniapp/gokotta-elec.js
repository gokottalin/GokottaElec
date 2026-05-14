(function () {
  const api = {
    samples: "/api/elec/samples",
    build: "/api/elec/build",
    handoff: "/api/elec/llm-handoff"
  };

  const fallbackSamples = [
    {
      id: "sample-01-voltage-divider",
      title: "Sample 01 - 电阻分压",
      source: `电路 WEB_SAMPLE_01_VOLTAGE_DIVIDER 版本 0.1.0。

网络 GND 是 ground，说明=全局参考地。
网络 VIN 是 input，说明=输入电压。
网络 VOUT 是 output，说明=分压输出节点。

器件 V1 是 VOLTAGE_SOURCE_DC，参数{voltage=5V}。
器件 R1 是 RESISTOR，参数{resistance=10kΩ}。
器件 R2 是 RESISTOR，参数{resistance=10kΩ}。

连接 VIN: V1.POS, R1.A。
连接 VOUT: R1.B, R2.A。
连接 GND: V1.NEG, R2.B。

约束 R1 必须 terminal_connected(A,B)。
约束 R2 必须 terminal_connected(A,B)。`
    }
  ];

  const el = {
    shell: document.querySelector("#appShell"),
    sampleSelect: document.querySelector("#sampleSelect"),
    sampleTitle: document.querySelector("#sampleTitle"),
    cnlInput: document.querySelector("#cnlInput"),
    renderButton: document.querySelector("#renderButton"),
    clearButton: document.querySelector("#clearButton"),
    copyButton: document.querySelector("#copyButton"),
    copyBasicHandoffButton: document.querySelector("#copyBasicHandoffButton"),
    copyFullHandoffButton: document.querySelector("#copyFullHandoffButton"),
    fitButton: document.querySelector("#fitButton"),
    downloadSvgButton: document.querySelector("#downloadSvgButton"),
    copyIrButton: document.querySelector("#copyIrButton"),
    circuitResultBar: document.querySelector("#circuitResultBar"),
    circuitSelect: document.querySelector("#circuitSelect"),
    circuitSummary: document.querySelector("#circuitSummary"),
    svgPreview: document.querySelector("#svgPreview"),
    diagnosticsLog: document.querySelector("#diagnosticsLog"),
    irViewer: document.querySelector("#irViewer")
  };

  let samples = fallbackSamples;
  let currentSvg = "";
  let currentSvgFilename = "gokottaelec.svg";
  let currentIr = null;
  let currentBuildResult = null;

  const logClasses = ["ge-log-info", "ge-log-loading", "ge-log-success", "ge-log-warning", "ge-log-error"];

  function setShellState(state) {
    el.shell.dataset.state = state;
  }

  function setLog(value, level = "info") {
    const safeLevel = ["loading", "success", "warning", "error"].includes(level) ? level : "info";
    el.diagnosticsLog.textContent = value || "";
    el.diagnosticsLog.classList.remove(...logClasses);
    el.diagnosticsLog.classList.add(`ge-log-${safeLevel}`);
  }

  function setPreviewState(state, isEmpty) {
    const safeState = ["idle", "loading", "success", "warning", "error", "backend"].includes(state) ? state : "idle";
    el.svgPreview.className = `${isEmpty ? "ge-preview-empty " : ""}ge-preview-${safeState}`.trim();
    setShellState(safeState);
  }

  function setPreviewEmpty(message, state = "idle") {
    currentSvg = "";
    currentSvgFilename = "gokottaelec.svg";
    currentBuildResult = null;
    el.downloadSvgButton.disabled = true;
    setPreviewState(state, true);
    el.svgPreview.textContent = message;
    setCircuitChoices([]);
  }

  function setIr(ir) {
    currentIr = ir || null;
    el.copyIrButton.disabled = !currentIr;
    el.irViewer.textContent = currentIr ? JSON.stringify(currentIr, null, 2) : "{}";
  }

  function normalizeBuildResponse(data) {
    const rawCircuits = Array.isArray(data.circuits) ? data.circuits : [];
    const artifacts = data.artifacts || {};
    const circuits = rawCircuits.length
      ? rawCircuits.map((circuit, index) => normalizeCircuit(circuit, data, index, rawCircuits.length))
      : [];
    if (!circuits.length && (artifacts.svg || artifacts.ir || artifacts.ercText)) {
      circuits.push(normalizeCircuit({
        id: circuitIdFromIr(artifacts.ir) || "CIRCUIT_1",
        ok: Boolean(data.ok),
        svg: artifacts.svg || "",
        ir: artifacts.ir || null,
        erc: artifacts.ercText || ""
      }, data, 0, 1));
    }
    return {
      ok: Boolean(data.ok),
      circuits,
      diagnostics: Array.isArray(data.diagnostics) ? data.diagnostics : [],
      raw: data
    };
  }

  function circuitIdFromIr(ir) {
    return ir?.circuit_id || ir?.circuit?.id || ir?.circuit?.circuit_id || "";
  }

  function diagnosticBelongsToCircuit(diagnostic, circuitId, circuitCount) {
    if (!diagnostic?.target) return true;
    if (diagnostic.target === circuitId) return true;
    return circuitCount === 1;
  }

  function normalizeCircuit(circuit, data, index, circuitCount) {
    const id = circuit?.id || circuitIdFromIr(circuit?.ir) || `CIRCUIT_${index + 1}`;
    const topDiagnostics = Array.isArray(data.diagnostics) ? data.diagnostics : [];
    const diagnostics = Array.isArray(circuit?.diagnostics) ? circuit.diagnostics : [];
    const warnings = Array.isArray(circuit?.warnings) ? circuit.warnings : [];
    const circuitDiagnostics = topDiagnostics.filter((item) => diagnosticBelongsToCircuit(item, id, circuitCount));
    return {
      id,
      ok: circuit?.ok !== false,
      svg: circuit?.svg || "",
      ir: circuit?.ir || null,
      ercText: circuit?.erc || "",
      diagnostics: [...diagnostics, ...warnings, ...circuitDiagnostics]
    };
  }

  function diagnosticsToText(circuit) {
    const parts = [];
    if (circuit.ercText) parts.push(circuit.ercText.trim());
    if (circuit.diagnostics && circuit.diagnostics.length) {
      parts.push(circuit.diagnostics.map((item) => {
        const line = item.line ? ` line ${item.line}` : "";
        const target = item.target ? ` [${item.target}]` : "";
        return `${item.level || "INFO"}${line}${target}: ${item.code || ""} ${item.message || ""}`.trim();
      }).join("\n"));
    }
    if (!parts.length) parts.push("OK");
    return parts.join("\n\n");
  }

  function circuitSeverity(circuit) {
    const levels = (circuit.diagnostics || []).map((item) => String(item.level || "").toUpperCase());
    const ercText = String(circuit.ercText || "").toUpperCase();
    if (circuit.ok === false || levels.includes("ERROR") || ercText.includes("ERROR")) return "error";
    if (levels.includes("WARNING") || ercText.includes("WARNING")) return "warning";
    return "success";
  }

  function circuitLabel(circuit, index, total) {
    return `${String(index + 1).padStart(2, "0")} / ${total} - ${circuit.id}`;
  }

  function setCircuitChoices(circuits) {
    const total = circuits.length;
    el.circuitResultBar.hidden = total <= 1;
    el.circuitSelect.innerHTML = "";
    el.circuitSummary.textContent = total ? `1 / ${total}` : "0 / 0";
    if (total <= 1) return;
    el.circuitSelect.innerHTML = circuits.map((circuit, index) => (
      `<option value="${index}">${circuitLabel(circuit, index, total)}</option>`
    )).join("");
  }

  function displayCircuit(index) {
    const circuits = currentBuildResult?.circuits || [];
    const circuit = circuits[index];
    if (!circuit) {
      setPreviewEmpty("后端没有返回可显示的电路。", "backend");
      setIr(null);
      return;
    }

    currentSvg = circuit.svg || "";
    currentSvgFilename = `${circuit.id || "gokottaelec"}.svg`;
    const severity = circuitSeverity(circuit);
    el.circuitSelect.value = String(index);
    el.circuitSummary.textContent = `${index + 1} / ${circuits.length}`;
    setPreviewState(currentSvg ? severity : "backend", !currentSvg);
    el.svgPreview.innerHTML = currentSvg || "后端没有返回 SVG。";
    el.downloadSvgButton.disabled = !currentSvg;
    setIr(circuit.ir);
    setLog(diagnosticsToText(circuit), severity);
  }

  async function loadSamples() {
    try {
      const response = await fetch(api.samples);
      if (!response.ok) throw new Error("samples api not ready");
      const data = await response.json();
      samples = Array.isArray(data.samples) && data.samples.length ? data.samples : fallbackSamples;
    } catch {
      samples = fallbackSamples;
      setLog("Sample API 未接入，已加载内置最小示例。", "warning");
    }

    el.sampleSelect.innerHTML = `<option value="">加载 Sample</option>` + samples.map((sample) => (
      `<option value="${sample.id}">${sample.title || sample.id}</option>`
    )).join("");
    updateSampleTitle(samples[0]);
  }

  async function buildCircuit() {
    const source = el.cnlInput.value.trim();
    if (!source) {
      setPreviewEmpty("请先输入 CNL 文本", "warning");
      setLog("没有输入内容。", "warning");
      setIr(null);
      return;
    }

    el.renderButton.disabled = true;
    setLog("正在调用 /api/elec/build ...", "loading");
    setPreviewEmpty("正在生成 SVG 原理图...", "loading");
    setIr(null);

    try {
      const response = await fetch(api.build, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          source,
          inputType: "cnl",
          options: {
            runErc: true,
            renderSvg: true,
            allowWarnings: true
          }
        })
      });
      const data = await response.json();
      const result = normalizeBuildResponse(data);
      if (!response.ok || !result.ok || !result.circuits.length) throw data;

      currentBuildResult = result;
      setCircuitChoices(result.circuits);
      displayCircuit(0);
    } catch (error) {
      const errorText = formatError(error);
      const state = errorText.includes("接口未接入") ? "backend" : "error";
      setPreviewEmpty("接口未接入或生成失败", state);
      setIr(null);
      setLog(errorText, "error");
    } finally {
      el.renderButton.disabled = false;
    }
  }

  async function copyText(text, successMessage, emptyMessage) {
    if (!text) {
      setLog(emptyMessage || "没有可复制内容。", "warning");
      return false;
    }
    try {
      if (!navigator.clipboard?.writeText) throw new Error("当前浏览器不支持剪贴板写入");
      await navigator.clipboard.writeText(text);
      setLog(successMessage, "success");
      return true;
    } catch (error) {
      const message = error?.message || "未知错误";
      setLog(`复制失败：${message}`, "error");
      return false;
    }
  }

  async function copyLlmHandoff(mode) {
    const full = mode === "full";
    const button = full ? el.copyFullHandoffButton : el.copyBasicHandoffButton;
    const label = full ? "完整 LLM 对接 Markdown" : "基础 LLM 对接 Markdown";

    button.disabled = true;
    setLog(`正在调用 ${api.handoff}?mode=${mode} ...`);

    try {
      const response = await fetch(`${api.handoff}?mode=${encodeURIComponent(mode)}`);
      const contentType = response.headers.get("content-type") || "";
      const data = contentType.includes("application/json")
        ? await response.json()
        : { ok: response.ok, markdown: await response.text() };

      if (!response.ok || data.ok === false || !data.markdown) throw data;
      await copyText(data.markdown, `已复制：${label}`, "LLM 对接内容为空。");
    } catch (error) {
      setLog(formatHandoffError(error, mode), "error");
    } finally {
      button.disabled = false;
    }
  }

  function formatError(error) {
    if (error && Array.isArray(error.diagnostics)) {
      return error.diagnostics.map((item) => `${item.level || "ERROR"}: ${item.message || item.code || "未知错误"}`).join("\n");
    }
    if (error && error.message) return `接口未接入或请求失败：${error.message}`;
    return "接口未接入或请求失败。请确认 GokottaMaker 后端已实现 POST /api/elec/build。";
  }

  function formatHandoffError(error, mode) {
    if (error && Array.isArray(error.diagnostics)) {
      return error.diagnostics.map((item) => `${item.level || "ERROR"}: ${item.message || item.code || "未知错误"}`).join("\n");
    }
    if (error && error.message) return `LLM 对接复制失败：${error.message}`;
    return `LLM 对接接口未接入。请确认 GokottaMaker 后端已实现 GET /api/elec/llm-handoff?mode=${mode}。`;
  }

  function downloadText(filename, text, type) {
    const blob = new Blob([text], { type });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = filename;
    anchor.click();
    URL.revokeObjectURL(url);
  }

  el.sampleSelect.addEventListener("change", () => {
    const sample = samples.find((item) => item.id === el.sampleSelect.value);
    if (sample) {
      el.cnlInput.value = sample.source || "";
      updateSampleTitle(sample);
    }
  });

  function updateSampleTitle(sample) {
    const title = sample?.title || sample?.id || "内置最小示例";
    el.sampleTitle.textContent = `当前 Sample：${title}`;
    el.sampleTitle.title = title;
    el.sampleSelect.title = title;
  }

  el.renderButton.addEventListener("click", buildCircuit);
  el.circuitSelect.addEventListener("change", () => displayCircuit(Number(el.circuitSelect.value || 0)));
  el.copyBasicHandoffButton.addEventListener("click", () => copyLlmHandoff("basic"));
  el.copyFullHandoffButton.addEventListener("click", () => copyLlmHandoff("full"));
  el.clearButton.addEventListener("click", () => {
    el.cnlInput.value = "";
    setPreviewEmpty("等待生成预览", "idle");
    setLog("已清空。");
    setIr(null);
  });
  el.copyButton.addEventListener("click", () => copyText(el.cnlInput.value, "已复制输入 CNL。", "没有可复制的 CNL 输入。"));
  el.copyIrButton.addEventListener("click", () => copyText(el.irViewer.textContent, "已复制 IR JSON。", "没有可复制的 IR JSON。"));
  el.fitButton.addEventListener("click", () => el.svgPreview.scrollTo({ left: 0, top: 0, behavior: "smooth" }));
  el.downloadSvgButton.addEventListener("click", () => {
    try {
      if (!currentSvg) throw new Error("没有可下载的 SVG。");
      downloadText(currentSvgFilename, currentSvg, "image/svg+xml;charset=utf-8");
      setLog(`已下载 SVG：${currentSvgFilename}`, "success");
    } catch (error) {
      setLog(error?.message || "下载 SVG 失败。", "error");
    }
  });

  loadSamples();
  el.cnlInput.value = fallbackSamples[0].source;
})();
