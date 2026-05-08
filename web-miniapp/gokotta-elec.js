(function () {
  const api = {
    samples: "/api/elec/samples",
    build: "/api/elec/build"
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
    sampleSelect: document.querySelector("#sampleSelect"),
    cnlInput: document.querySelector("#cnlInput"),
    renderButton: document.querySelector("#renderButton"),
    clearButton: document.querySelector("#clearButton"),
    copyButton: document.querySelector("#copyButton"),
    fitButton: document.querySelector("#fitButton"),
    downloadSvgButton: document.querySelector("#downloadSvgButton"),
    copyIrButton: document.querySelector("#copyIrButton"),
    svgPreview: document.querySelector("#svgPreview"),
    diagnosticsLog: document.querySelector("#diagnosticsLog"),
    irViewer: document.querySelector("#irViewer")
  };

  let samples = fallbackSamples;
  let currentSvg = "";
  let currentIr = null;

  function setLog(value, isError) {
    el.diagnosticsLog.textContent = value || "";
    el.diagnosticsLog.classList.toggle("ge-error", Boolean(isError));
  }

  function setPreviewEmpty(message) {
    currentSvg = "";
    el.downloadSvgButton.disabled = true;
    el.svgPreview.className = "ge-preview-empty";
    el.svgPreview.textContent = message;
  }

  function setIr(ir) {
    currentIr = ir || null;
    el.copyIrButton.disabled = !currentIr;
    el.irViewer.textContent = currentIr ? JSON.stringify(currentIr, null, 2) : "{}";
  }

  function normalizeBuildResponse(data) {
    const firstCircuit = Array.isArray(data.circuits) ? data.circuits[0] : null;
    const artifacts = data.artifacts || {};
    return {
      ok: Boolean(data.ok),
      svg: artifacts.svg || firstCircuit?.svg || "",
      ir: artifacts.ir || firstCircuit?.ir || null,
      ercText: artifacts.ercText || firstCircuit?.erc || "",
      diagnostics: data.diagnostics || firstCircuit?.warnings || [],
      raw: data
    };
  }

  function diagnosticsToText(result) {
    const parts = [];
    if (result.ercText) parts.push(result.ercText.trim());
    if (result.diagnostics && result.diagnostics.length) {
      parts.push(result.diagnostics.map((item) => {
        const line = item.line ? ` line ${item.line}` : "";
        const target = item.target ? ` [${item.target}]` : "";
        return `${item.level || "INFO"}${line}${target}: ${item.code || ""} ${item.message || ""}`.trim();
      }).join("\n"));
    }
    if (!parts.length) parts.push("OK");
    return parts.join("\n\n");
  }

  async function loadSamples() {
    try {
      const response = await fetch(api.samples);
      if (!response.ok) throw new Error("samples api not ready");
      const data = await response.json();
      samples = Array.isArray(data.samples) && data.samples.length ? data.samples : fallbackSamples;
    } catch {
      samples = fallbackSamples;
      setLog("Sample API 未接入，已加载内置最小示例。");
    }

    el.sampleSelect.innerHTML = `<option value="">加载 Sample</option>` + samples.map((sample) => (
      `<option value="${sample.id}">${sample.title || sample.id}</option>`
    )).join("");
  }

  async function buildCircuit() {
    const source = el.cnlInput.value.trim();
    if (!source) {
      setPreviewEmpty("请先输入 CNL 文本");
      setLog("没有输入内容。", true);
      setIr(null);
      return;
    }

    el.renderButton.disabled = true;
    setLog("正在调用 /api/elec/build ...");
    setPreviewEmpty("正在生成 SVG 原理图...");
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
      if (!response.ok || !result.ok) throw data;

      currentSvg = result.svg;
      el.svgPreview.className = "";
      el.svgPreview.innerHTML = currentSvg || "后端没有返回 SVG。";
      el.downloadSvgButton.disabled = !currentSvg;
      setIr(result.ir);
      setLog(diagnosticsToText(result));
    } catch (error) {
      setPreviewEmpty("接口未接入或生成失败");
      setIr(null);
      setLog(formatError(error), true);
    } finally {
      el.renderButton.disabled = false;
    }
  }

  function formatError(error) {
    if (error && Array.isArray(error.diagnostics)) {
      return error.diagnostics.map((item) => `${item.level || "ERROR"}: ${item.message || item.code || "未知错误"}`).join("\n");
    }
    if (error && error.message) return `接口未接入或请求失败：${error.message}`;
    return "接口未接入或请求失败。请确认 GokottaMaker 后端已实现 POST /api/elec/build。";
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
    if (sample) el.cnlInput.value = sample.source || "";
  });

  el.renderButton.addEventListener("click", buildCircuit);
  el.clearButton.addEventListener("click", () => {
    el.cnlInput.value = "";
    setPreviewEmpty("等待生成预览");
    setLog("已清空。");
    setIr(null);
  });
  el.copyButton.addEventListener("click", () => navigator.clipboard?.writeText(el.cnlInput.value));
  el.copyIrButton.addEventListener("click", () => navigator.clipboard?.writeText(el.irViewer.textContent));
  el.fitButton.addEventListener("click", () => el.svgPreview.scrollTo({ left: 0, top: 0, behavior: "smooth" }));
  el.downloadSvgButton.addEventListener("click", () => {
    if (currentSvg) downloadText("gokottaelec.svg", currentSvg, "image/svg+xml;charset=utf-8");
  });

  loadSamples();
  el.cnlInput.value = fallbackSamples[0].source;
})();
