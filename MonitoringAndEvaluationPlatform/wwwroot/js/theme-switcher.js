/**
 * Theme Switcher - Modern Theme System
 * Handles dynamic theme switching with smooth transitions
 */

class ThemeSwitcher {
    constructor() {
        this.currentTheme = this.getCurrentTheme();
        this.init();
    }

    init() {
        this.bindEvents();
        this.applyTheme(this.currentTheme);
        this.addThemeMetaTags();
    }

    bindEvents() {
        // Theme option click handlers
        document.addEventListener('click', (e) => {
            if (e.target.closest('.theme-option')) {
                e.preventDefault();
                const themeOption = e.target.closest('.theme-option');
                const themeId = themeOption.dataset.theme;
                const themeName = themeOption.dataset.themeName;
                
                this.switchTheme(themeId, themeName);
            }
        });

        // Listen for theme changes from other tabs/windows
        window.addEventListener('storage', (e) => {
            if (e.key === 'selectedTheme' && e.newValue !== this.currentTheme) {
                this.currentTheme = e.newValue;
                this.applyTheme(this.currentTheme, false);
            }
        });
    }

    getCurrentTheme() {
        // Try to get theme from cookie first
        const cookieTheme = this.getCookie('SelectedTheme');
        if (cookieTheme) {
            return cookieTheme;
        }

        // Try localStorage as fallback
        const localTheme = localStorage.getItem('selectedTheme');
        if (localTheme) {
            return localTheme;
        }

        // Default to classic theme
        return 'classic';
    }

    getCookie(name) {
        const value = `; ${document.cookie}`;
        const parts = value.split(`; ${name}=`);
        if (parts.length === 2) return parts.pop().split(';').shift();
        return null;
    }

    setCookie(name, value, days = 365) {
        const expires = new Date();
        expires.setTime(expires.getTime() + (days * 24 * 60 * 60 * 1000));
        document.cookie = `${name}=${value};expires=${expires.toUTCString()};path=/;SameSite=Lax`;
    }

    async switchTheme(themeId, themeName) {
        if (themeId === this.currentTheme) return;

        this.showSwitchingIndicator();

        try {
            // Send request to server to update theme preference
            const response = await fetch('/Theme/SetTheme', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'X-Requested-With': 'XMLHttpRequest'
                },
                body: `themeId=${encodeURIComponent(themeId)}`
            });

            if (response.ok) {
                this.currentTheme = themeId;
                this.applyTheme(themeId);
                this.updateThemeSwitcherUI(themeId, themeName);
                
                // Store in localStorage for consistency
                localStorage.setItem('selectedTheme', themeId);

                this.showSuccessMessage();
            } else {
                throw new Error('Failed to update theme preference');
            }
        } catch (error) {
            console.error('Theme switching error:', error);
            this.showErrorMessage();
        } finally {
            this.hideSwitchingIndicator();
        }
    }

    applyTheme(themeId, animate = true) {
        const body = document.body;
        
        if (animate) {
            body.style.transition = 'all 0.3s ease';
        }

        // Remove existing theme classes
        body.classList.remove('theme-classic', 'theme-modern');
        
        // Add new theme class
        body.classList.add(`theme-${themeId}`);

        // Update theme-specific stylesheets
        this.updateThemeStylesheets(themeId);

        // Apply theme to existing elements
        this.applyThemeToElements(themeId);

        // Store current theme
        this.currentTheme = themeId;

        if (animate) {
            setTimeout(() => {
                body.style.transition = '';
            }, 300);
        }
    }

    updateThemeStylesheets(themeId) {
        // Remove existing theme stylesheets
        const existingThemeLinks = document.querySelectorAll('link[data-theme-css]');
        existingThemeLinks.forEach(link => link.remove());

        // Add new theme stylesheet
        const themeLink = document.createElement('link');
        themeLink.rel = 'stylesheet';
        themeLink.href = `/css/themes/${themeId}-theme.css`;
        themeLink.setAttribute('data-theme-css', themeId);
        document.head.appendChild(themeLink);
    }

    applyThemeToElements(themeId) {
        // Update specific elements that might need theme-specific classes
        const cards = document.querySelectorAll('.card');
        const buttons = document.querySelectorAll('.btn');
        const alerts = document.querySelectorAll('.alert');

        // Add fade-in animation for modern theme
        if (themeId === 'modern') {
            cards.forEach((card, index) => {
                setTimeout(() => {
                    card.classList.add('fade-in-up');
                }, index * 50);
            });
        } else {
            cards.forEach(card => {
                card.classList.remove('fade-in-up');
            });
        }
    }

    updateThemeSwitcherUI(themeId, themeName) {
        // Update the theme switcher button text
        const themeNameElement = document.querySelector('.theme-switcher .theme-name');
        if (themeNameElement) {
            themeNameElement.textContent = themeName;
        }

        // Update active state in dropdown
        const themeOptions = document.querySelectorAll('.theme-option');
        themeOptions.forEach(option => {
            const checkIcon = option.querySelector('.fa-check');
            if (option.dataset.theme === themeId) {
                option.classList.add('active');
                if (!checkIcon) {
                    const check = document.createElement('i');
                    check.className = 'fas fa-check text-success';
                    option.querySelector('.d-flex').appendChild(check);
                }
            } else {
                option.classList.remove('active');
                if (checkIcon) {
                    checkIcon.remove();
                }
            }
        });
    }

    showSwitchingIndicator() {
        // Create loading indicator if it doesn't exist
        if (!document.querySelector('.theme-switch-loading')) {
            const loading = document.createElement('div');
            loading.className = 'theme-switch-loading';
            loading.innerHTML = `
                <div class="d-flex align-items-center">
                    <div class="spinner-border spinner-border-sm me-3" role="status">
                        <span class="visually-hidden">Loading...</span>
                    </div>
                    <span>Switching theme...</span>
                </div>
            `;
            document.body.appendChild(loading);
        }

        document.querySelector('.theme-switch-loading').classList.add('active');
        document.body.classList.add('theme-switching');
    }

    hideSwitchingIndicator() {
        const loading = document.querySelector('.theme-switch-loading');
        if (loading) {
            loading.classList.remove('active');
        }
        document.body.classList.remove('theme-switching');
    }

    showSuccessMessage() {
        this.showToast('Theme changed successfully!', 'success');
    }

    showErrorMessage() {
        this.showToast('Failed to change theme. Please try again.', 'error');
    }

    showToast(message, type = 'info') {
        // Create toast container if it doesn't exist
        let toastContainer = document.querySelector('.toast-container');
        if (!toastContainer) {
            toastContainer = document.createElement('div');
            toastContainer.className = 'toast-container position-fixed top-0 end-0 p-3';
            toastContainer.style.zIndex = '9999';
            document.body.appendChild(toastContainer);
        }

        // Create toast
        const toastId = 'toast_' + Date.now();
        const toastHtml = `
            <div id="${toastId}" class="toast" role="alert">
                <div class="toast-header">
                    <i class="fas fa-${type === 'success' ? 'check-circle text-success' : 'exclamation-triangle text-warning'} me-2"></i>
                    <strong class="me-auto">Theme Switcher</strong>
                    <button type="button" class="btn-close" data-bs-dismiss="toast"></button>
                </div>
                <div class="toast-body">
                    ${message}
                </div>
            </div>
        `;

        toastContainer.insertAdjacentHTML('beforeend', toastHtml);

        // Initialize and show toast
        const toastElement = document.getElementById(toastId);
        const toast = new bootstrap.Toast(toastElement, {
            autohide: true,
            delay: 3000
        });
        toast.show();

        // Clean up after toast is hidden
        toastElement.addEventListener('hidden.bs.toast', () => {
            toastElement.remove();
        });
    }

    addThemeMetaTags() {
        // Add theme-color meta tag
        let themeColorMeta = document.querySelector('meta[name="theme-color"]');
        if (!themeColorMeta) {
            themeColorMeta = document.createElement('meta');
            themeColorMeta.name = 'theme-color';
            document.head.appendChild(themeColorMeta);
        }

        // Set theme color based on current theme
        const themeColors = {
            'classic': '#0d6efd',
            'modern': '#667eea'
        };
        themeColorMeta.content = themeColors[this.currentTheme] || themeColors.classic;
    }

    // Public method to get current theme
    getTheme() {
        return this.currentTheme;
    }

    // Public method to set theme programmatically
    setTheme(themeId) {
        const availableThemes = ['classic', 'modern'];
        if (availableThemes.includes(themeId)) {
            this.switchTheme(themeId, themeId === 'classic' ? 'Classic Theme' : 'Modern Theme');
        }
    }
}

// Initialize theme switcher when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.themeSwitcher = new ThemeSwitcher();
});

// Expose theme switcher globally
window.ThemeSwitcher = ThemeSwitcher;