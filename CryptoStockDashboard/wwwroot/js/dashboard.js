"use strict";

// SignalR Dashboard Connection
const DashboardApp = {
    connection: null,

    init: function () {
        this.setupSignalR();
    },

    setupSignalR: function () {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/dashboardHub")
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        this.connection.on("ReceiveDashboardUpdate", (data) => {
            this.handleDashboardUpdate(data);
        });

        this.connection.onreconnecting(() => {
            this.updateConnectionStatus("Reconnecting...", "bg-warning");
        });

        this.connection.onreconnected(() => {
            this.updateConnectionStatus("Connected", "bg-success");
        });

        this.connection.onclose(() => {
            this.updateConnectionStatus("Disconnected", "bg-danger");
            setTimeout(() => this.startConnection(), 5000);
        });

        this.startConnection();
    },

    startConnection: async function () {
        try {
            await this.connection.start();
            this.updateConnectionStatus("Live", "bg-success");
            console.log("SignalR connected");
        } catch (err) {
            this.updateConnectionStatus("Offline", "bg-danger");
            console.warn("SignalR connection failed, retrying...", err);
            setTimeout(() => this.startConnection(), 5000);
        }
    },

    updateConnectionStatus: function (text, badgeClass) {
        const el = document.getElementById("connectionStatus");
        if (el) {
            el.className = `badge ${badgeClass} me-2`;
            el.innerHTML = `<i class="bi bi-circle-fill me-1"></i>${text}`;
        }
    },

    handleDashboardUpdate: function (data) {
        // Update the last updated timestamp
        const lastUpdatedEl = document.getElementById("lastUpdated");
        if (lastUpdatedEl) {
            const now = new Date();
            lastUpdatedEl.textContent = `Updated: ${now.toLocaleTimeString()}`;
        }

        const statLastUpdated = document.getElementById("statLastUpdated");
        if (statLastUpdated) {
            const now = new Date();
            statLastUpdated.innerHTML = `<span class="live-dot"></span>${now.toUTCString().split(' ')[4]} UTC`;
        }

        // Update crypto table on dashboard
        if (data.cryptoAssets) {
            this.updateCryptoTable(data.cryptoAssets);
        }

        // Update stock table on dashboard
        if (data.stockQuotes) {
            this.updateStockTable(data.stockQuotes);
        }

        // Update predictions
        if (data.predictions) {
            this.updatePredictions(data.predictions);
        }

        // Flash effect for updated data
        this.flashUpdatedRows();
    },

    updateCryptoTable: function (cryptoAssets) {
        const table = document.getElementById("cryptoTable");
        if (!table) return;

        const tbody = table.querySelector("tbody");
        if (!tbody) return;

        tbody.innerHTML = cryptoAssets.slice(0, 5).map(c => {
            const changeClass = c.priceChangePercent24h >= 0 ? "price-up" : "price-down";
            const changeIcon = c.priceChangePercent24h >= 0 ? "bi-caret-up-fill" : "bi-caret-down-fill";
            const imgHtml = c.imageUrl ? `<img src="${c.imageUrl}" class="crypto-icon me-2" alt="${c.symbol}" />` : "";
            return `<tr class="fade-in">
                <td>${imgHtml}<strong>${c.symbol}</strong><small class="text-muted ms-1">${c.name}</small></td>
                <td class="text-end fw-semibold">$${this.formatNumber(c.currentPrice)}</td>
                <td class="text-end ${changeClass}"><i class="bi ${changeIcon}"></i> ${c.priceChangePercent24h.toFixed(2)}%</td>
                <td class="text-end text-muted">$${this.formatLargeNumber(c.marketCap)}</td>
            </tr>`;
        }).join("");

        // Update stat card
        if (cryptoAssets.length > 0) {
            const statEl = document.getElementById("statTopCrypto");
            if (statEl) {
                statEl.textContent = `${cryptoAssets[0].symbol} $${this.formatNumber(cryptoAssets[0].currentPrice)}`;
            }
        }
    },

    updateStockTable: function (stocks) {
        const table = document.getElementById("stockTable");
        if (!table) return;

        const tbody = table.querySelector("tbody");
        if (!tbody) return;

        tbody.innerHTML = stocks.slice(0, 5).map(s => {
            const changeClass = s.change >= 0 ? "price-up" : "price-down";
            const changeIcon = s.change >= 0 ? "bi-caret-up-fill" : "bi-caret-down-fill";
            return `<tr class="fade-in">
                <td><strong>${s.symbol}</strong><small class="text-muted ms-1">${s.name}</small></td>
                <td class="text-end fw-semibold">$${this.formatNumber(s.price)}</td>
                <td class="text-end ${changeClass}"><i class="bi ${changeIcon}"></i> ${s.changePercent.toFixed(2)}%</td>
                <td class="text-end text-muted">${this.formatLargeNumber(s.volume)}</td>
            </tr>`;
        }).join("");

        // Update stat card
        if (stocks.length > 0) {
            const statEl = document.getElementById("statTopStock");
            if (statEl) {
                statEl.textContent = `${stocks[0].symbol} $${this.formatNumber(stocks[0].price)}`;
            }
        }
    },

    updatePredictions: function (predictions) {
        const grid = document.getElementById("predictionsGrid");
        if (!grid) return;

        grid.innerHTML = predictions.slice(0, 6).map(p => {
            const signalClass = p.signal === "Buy" ? "signal-buy" : p.signal === "Sell" ? "signal-sell" : "signal-hold";
            const priceClass = p.predictedChangePercent >= 0 ? "price-up" : "price-down";
            const confidence = (p.confidence * 100).toFixed(0);
            return `<div class="col-md-4 col-lg-2">
                <div class="dashboard-card text-center p-3 fade-in">
                    <div class="fw-bold">${p.symbol}</div>
                    <small class="text-muted">${p.assetType}</small>
                    <div class="my-2"><span class="badge ${signalClass} px-3 py-1">${p.signal}</span></div>
                    <div class="${priceClass} fw-semibold">${p.predictedChangePercent >= 0 ? "+" : ""}${p.predictedChangePercent.toFixed(2)}%</div>
                    <div class="mt-2">
                        <div class="confidence-bar">
                            <div class="confidence-fill bg-info" style="width: ${confidence}%"></div>
                        </div>
                        <small class="text-muted">${confidence}% confidence</small>
                    </div>
                </div>
            </div>`;
        }).join("");

        // Update buy signals count
        const buyCount = predictions.filter(p => p.signal === "Buy").length;
        const statEl = document.getElementById("statBuySignals");
        if (statEl) {
            statEl.textContent = buyCount.toString();
        }
    },

    flashUpdatedRows: function () {
        document.querySelectorAll(".fade-in").forEach(el => {
            el.style.opacity = "0.5";
            setTimeout(() => { el.style.transition = "opacity 0.5s"; el.style.opacity = "1"; }, 50);
        });
    },

    formatNumber: function (num) {
        return Number(num).toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    },

    formatLargeNumber: function (num) {
        if (num >= 1e12) return (num / 1e12).toFixed(2) + "T";
        if (num >= 1e9) return (num / 1e9).toFixed(2) + "B";
        if (num >= 1e6) return (num / 1e6).toFixed(2) + "M";
        if (num >= 1e3) return (num / 1e3).toFixed(2) + "K";
        return num.toFixed(2);
    }
};

// Initialize when DOM is ready
document.addEventListener("DOMContentLoaded", function () {
    DashboardApp.init();
});
