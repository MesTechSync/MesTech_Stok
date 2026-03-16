/**
 * MesTech Chart.js Interop — Blazor <-> Chart.js bridge
 * G-08: Dashboard Charts
 */
window.createChart = (id, type, data, options) => {
  const existingChart = Chart.getChart(id);
  if (existingChart) existingChart.destroy();
  const ctx = document.getElementById(id);
  if (ctx) {
    new Chart(ctx, {
      type,
      data,
      options: {
        ...options,
        responsive: true,
        maintainAspectRatio: false
      }
    });
  }
};

window.updateChart = (id, data) => {
  const chart = Chart.getChart(id);
  if (chart) {
    chart.data = data;
    chart.update();
  }
};

window.destroyChart = (id) => {
  const chart = Chart.getChart(id);
  if (chart) chart.destroy();
};
