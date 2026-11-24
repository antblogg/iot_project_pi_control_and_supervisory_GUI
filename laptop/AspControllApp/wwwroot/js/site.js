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
