const setpointForm = document.querySelector("#setpoint-form");

if (setpointForm instanceof HTMLFormElement) {
  const input = setpointForm.elements.namedItem("setpoint");
  const feedback = document.querySelector("#setpoint-feedback");
  const setpointDisplays = document.querySelectorAll("[data-setpoint-display]");
  const chartSetpoint = document.querySelector(".chart-setpoint");
  const chartDescription = document.querySelector("#chart-description");

  if (input instanceof HTMLInputElement && feedback instanceof HTMLElement) {
    setpointForm.addEventListener("submit", (event) => {
      event.preventDefault();
      if (!setpointForm.reportValidity()) return;

      const value = Number(input.value);
      setpointDisplays.forEach((display) => {
        const unit = document.createElement("span");
        unit.className = "small-value";
        unit.textContent = " °C";
        display.replaceChildren(document.createTextNode(value.toFixed(1)), unit);
      });
      if (chartSetpoint instanceof SVGLineElement) {
        const y = 238 - ((value - 58) / 32) * 208;
        chartSetpoint.setAttribute("y1", String(y));
        chartSetpoint.setAttribute("y2", String(y));
        chartSetpoint.style.display = value >= 58 && value <= 90 ? "" : "none";
      }
      if (chartDescription instanceof SVGDescElement) {
        chartDescription.textContent = `Simulated process temperature rises toward a ${value.toFixed(1)} degree Celsius setpoint.`;
      }
      feedback.textContent = "Setpoint updated in this preview.";
    });

    document.querySelectorAll("[data-setpoint-step]").forEach((button) => {
      button.addEventListener("click", () => {
        const step = Number(button.getAttribute("data-setpoint-step"));
        const nextValue = Math.min(200, Math.max(0, Number(input.value) + step));
        input.value = nextValue.toFixed(1);
        feedback.textContent = "";
      });
    });
  }
}

const loopToggle = document.querySelector("#loop-toggle");
const loopLabel = document.querySelector("#loop-state-label");

if (loopToggle instanceof HTMLButtonElement && loopLabel instanceof HTMLElement) {
  loopToggle.addEventListener("click", () => {
    const isRunning = loopToggle.getAttribute("aria-pressed") === "true";
    loopToggle.setAttribute("aria-pressed", String(!isRunning));
    loopToggle.textContent = isRunning ? "Resume" : "Pause";
    loopLabel.textContent = isRunning ? "Paused" : "Running";
  });
}

const settingsForm = document.querySelector("#settings-form");

if (settingsForm instanceof HTMLFormElement) {
  const feedback = document.querySelector("#settings-feedback");

  settingsForm.addEventListener("submit", (event) => {
    event.preventDefault();
    if (!settingsForm.reportValidity()) return;

    if (feedback instanceof HTMLElement) {
      feedback.textContent = "Values checked. Saving is not connected in this preview.";
    }
  });

  settingsForm.addEventListener("reset", () => {
    if (feedback instanceof HTMLElement) feedback.textContent = "";
  });
}
