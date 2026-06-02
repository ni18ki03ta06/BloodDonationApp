// site.js - Donora UI Interaction Script

document.addEventListener("DOMContentLoaded", () => {
    // 0. Dark Mode Toggler logic using data-theme attribute
    const themeToggleBtns = document.querySelectorAll(".theme-toggle-btn");
    const currentTheme = localStorage.getItem("theme");

    // Helper to apply theme
    const applyTheme = (theme) => {
        document.documentElement.setAttribute("data-theme", theme);
        themeToggleBtns.forEach(btn => {
            const icon = btn.querySelector("i");
            if (icon) {
                icon.className = theme === "dark" ? "bi bi-sun-fill" : "bi bi-moon-fill";
            }
        });
    };

    // Initial load check
    if (currentTheme) {
        applyTheme(currentTheme);
    } else {
        const prefersDark = window.matchMedia("(prefers-color-scheme: dark)").matches;
        applyTheme(prefersDark ? "dark" : "light");
    }

    // Toggle on click
    themeToggleBtns.forEach(btn => {
        btn.addEventListener("click", () => {
            const currentVal = document.documentElement.getAttribute("data-theme") || "light";
            const newTheme = currentVal === "dark" ? "light" : "dark";
            localStorage.setItem("theme", newTheme);
            applyTheme(newTheme);
        });
    });



    // 1. Password Visibility Toggles
    const setupPasswordToggle = (btnId, inputId, iconId) => {
        const toggleBtn = document.getElementById(btnId);
        const passwordInput = document.getElementById(inputId);
        const eyeIcon = document.getElementById(iconId);

        if (toggleBtn && passwordInput && eyeIcon) {
            toggleBtn.addEventListener("click", () => {
                const type = passwordInput.getAttribute("type") === "password" ? "text" : "password";
                passwordInput.setAttribute("type", type);
                
                // Toggle classes
                if (type === "text") {
                    eyeIcon.classList.remove("bi-eye-slash");
                    eyeIcon.classList.add("bi-eye");
                } else {
                    eyeIcon.classList.remove("bi-eye");
                    eyeIcon.classList.add("bi-eye-slash");
                }
            });
        }
    };

    setupPasswordToggle("btnTogglePassword", "txtPassword", "eyeIcon");
    setupPasswordToggle("btnToggleConfirmPassword", "txtConfirmPassword", "confirmEyeIcon");

    // 2. Click Ripple Effect on all Buttons
    const buttons = document.querySelectorAll(".btn");
    buttons.forEach(button => {
        button.addEventListener("click", function(e) {
            // Get click coordinates relative to button
            const rect = this.getBoundingClientRect();
            const x = e.clientX - rect.left;
            const y = e.clientY - rect.top;

            // Create ripple span element
            const rippleSpan = document.createElement("span");
            rippleSpan.classList.add("ripple");
            rippleSpan.style.left = `${x}px`;
            rippleSpan.style.top = `${y}px`;

            // Append to button
            this.appendChild(rippleSpan);

            // Remove ripple span after animation runs
            setTimeout(() => {
                rippleSpan.remove();
            }, 600);
        });
    });

    // 3. SignalR Real-Time Integrations
    if (typeof signalR !== "undefined") {
        const userId = document.body.dataset.userId;
        const userRole = document.body.dataset.userRole;

        let hubUrl = "/notificationHub";
        const params = [];
        if (userId) params.push(`userId=${encodeURIComponent(userId)}`);
        if (userRole) params.push(`role=${encodeURIComponent(userRole)}`);
        if (params.length > 0) {
            hubUrl += "?" + params.join("&");
        }

        const connection = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl)
            .withAutomaticReconnect()
            .build();

        async function startSignalR() {
            try {
                await connection.start();
                console.log("SignalR Connected to Donora Hub.");
            } catch (err) {
                console.error("SignalR Connection Failed: ", err);
                setTimeout(startSignalR, 5000);
            }
        }

        // Initialize connection
        startSignalR();

        // 3.1 Toast Notification Rendering Helper
        function getOrCreateToastContainer() {
            let container = document.querySelector(".donora-toast-container");
            if (!container) {
                container = document.createElement("div");
                container.className = "donora-toast-container";
                document.body.appendChild(container);
            }
            return container;
        }

        function showToast(title, message, isEmergency = false, clickUrl = null, durationMs = 8000) {
            const container = getOrCreateToastContainer();
            const toast = document.createElement("div");
            toast.className = `donora-toast${isEmergency ? " emergency" : ""}`;
            
            const now = new Date();
            const timeStr = now.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' });
            
            toast.innerHTML = `
                <div class="donora-toast-header">
                    <span class="title">${isEmergency ? '<i class="bi bi-exclamation-triangle-fill me-1"></i>' : ''}${title}</span>
                    <button class="donora-toast-close" type="button" aria-label="Close">&times;</button>
                </div>
                <div class="donora-toast-body">${message}</div>
                <div class="donora-toast-time">${timeStr}</div>
            `;
            
            // Navigate on click (ignoring the close button)
            toast.addEventListener("click", (e) => {
                if (e.target.classList.contains("donora-toast-close")) {
                    return;
                }
                if (clickUrl) {
                    window.location.href = clickUrl;
                }
            });
            
            // Close button click
            const closeBtn = toast.querySelector(".donora-toast-close");
            closeBtn.addEventListener("click", (e) => {
                e.stopPropagation();
                toast.style.animation = "toast-slide-out 0.3s ease forwards";
                setTimeout(() => {
                    toast.remove();
                }, 300);
            });
            
            container.appendChild(toast);
            
            // Auto dismissal
            setTimeout(() => {
                if (toast.parentNode) {
                    toast.style.animation = "toast-slide-out 0.3s ease forwards";
                    setTimeout(() => {
                        if (toast.parentNode) {
                            toast.remove();
                        }
                    }, 300);
                }
            }, durationMs);
        }

        // 3.2 Listen to targeted Donor notifications
        connection.on("ReceiveNotification", (notif) => {
            console.log("Targeted Notification Received: ", notif);
            const isEmergency = notif.title && notif.title.includes("EMERGENCY");
            
            // Update unread count badges in UI
            const badgeBell = document.getElementById("badgeUnreadNotificationsBell");
            const badgeNav = document.getElementById("badgeUnreadNotifications");
            
            [badgeBell, badgeNav].forEach(badge => {
                if (badge) {
                    badge.classList.remove("d-none");
                    let currentCount = parseInt(badge.textContent.trim()) || 0;
                    badge.textContent = currentCount + 1;
                }
            });
            
            // Render Burgundy & Gold Toast
            showToast(notif.title, notif.message, isEmergency, "/Donor/Notifications");
        });

        // 3.3 Listen to Admin dashboard alerts
        connection.on("ReceiveAdminAlert", (alertData) => {
            console.log("Admin Dashboard Alert Received: ", alertData);
            const isEmergency = alertData.status === "Emergency" || alertData.urgencyLevel === "Critical" || alertData.urgencyLevel === "Urgent";
            
            // Update counts in Admin interface
            const badgeBell = document.getElementById("badgeAdminEmergencyCount");
            const badgeSidebar = document.getElementById("badgeSidebarEmergencyCount");
            
            [badgeBell, badgeSidebar].forEach(badge => {
                if (badge) {
                    badge.classList.remove("d-none");
                    let currentCount = parseInt(badge.textContent.trim()) || 0;
                    if (badge.id === "badgeSidebarEmergencyCount") {
                        badge.textContent = `${currentCount + 1} Emergency`;
                    } else {
                        badge.textContent = currentCount + 1;
                    }
                }
            });
            
            const title = isEmergency ? `EMERGENCY Blood Request Match` : `New Blood Request`;
            const message = `A new ${alertData.urgencyLevel} request for ${alertData.bloodType} was created at ${alertData.hospital} (${alertData.city}) for patient ${alertData.patientName}.`;
            
            // Render Toast Alert
            showToast(title, message, isEmergency, `/Admin/MatchDonors/${alertData.id}`);
        });

        // 3.3b Listen to Donor Match alerts
        connection.on("ReceiveMatchAlert", (matchData) => {
            console.log("Donor Match Alert Received: ", matchData);
            const title = "MATCH FOUND: Blood Needed!";
            const message = `A blood request for type ${matchData.bloodType} matches your profile. Hospital: ${matchData.hospital}.`;
            showToast(title, message, true, `/Donor/Dashboard`, 6000);
        });

        // 3.4 Listen to Live Blood Stock updates
        connection.on("ReceiveInventoryUpdate", (update) => {
            console.log("Inventory Update Broadcast Received: ", update);
            
            // Public Inventory Page update
            const card = document.getElementById(`inventory-card-${update.id}`);
            if (card) {
                const body = card.querySelector(".inventory-card-body");
                if (body) {
                    body.style.backgroundColor = update.bgLight;
                }
                
                const badge = card.querySelector(".status-badge");
                if (badge) {
                    badge.className = `badge ${update.badgeClass} px-3 py-2 text-uppercase font-monospace small status-badge`;
                    badge.textContent = update.statusLabel;
                }
                
                const unitsEl = card.querySelector(".stock-units");
                if (unitsEl) {
                    unitsEl.textContent = update.unitsAvailable;
                    unitsEl.className = `stock-highlight ${update.textClass} stock-units`;
                }
                
                const reservedContainer = card.querySelector(".units-reserved-container");
                if (reservedContainer) {
                    if (update.unitsReserved > 0) {
                        reservedContainer.innerHTML = `<span class="badge bg-secondary units-reserved-badge">${update.unitsReserved} Units Reserved</span>`;
                    } else {
                        reservedContainer.innerHTML = "";
                    }
                }
                
                // Trigger Flash Effect
                card.classList.remove("flash-update-card");
                void card.offsetWidth; // Force Reflow
                card.classList.add("flash-update-card");
            }

            // Admin Inventory Panel update
            const row = document.getElementById(`inventory-row-${update.id}`);
            if (row) {
                const badge = row.querySelector(".status-badge");
                if (badge) {
                    badge.className = `badge ${update.badgeClass} px-3 py-2 font-monospace status-badge`;
                    badge.textContent = update.statusLabel;
                }
                
                const unitsEl = row.querySelector(".stock-units");
                if (unitsEl) {
                    unitsEl.textContent = update.unitsAvailable;
                    unitsEl.className = `text-center fw-bold fs-5 ${update.textClass} font-monospace stock-units`;
                }
                
                const reservedEl = row.querySelector(".units-reserved");
                if (reservedEl) {
                    reservedEl.textContent = update.unitsReserved;
                }
                
                const updatedEl = row.querySelector(".last-updated");
                if (updatedEl) {
                    updatedEl.textContent = update.lastUpdated;
                }
                
                const inputEl = row.querySelector(".input-units-field");
                if (inputEl) {
                    inputEl.value = update.unitsAvailable;
                }
                
                // Trigger Row Flash Effect
                row.classList.remove("flash-update-row");
                void row.offsetWidth; // Force Reflow
                row.classList.add("flash-update-row");
            }
        });
    }

    // 4. Sidebar Toggle Listener
    const sidebarToggleBtn = document.getElementById("sidebarToggle");
    if (sidebarToggleBtn) {
        sidebarToggleBtn.addEventListener("click", (e) => {
            e.preventDefault();
            const wrapper = document.getElementById("wrapper");
            if (wrapper) {
                wrapper.classList.toggle("toggled");
            }
        });
    }
});
