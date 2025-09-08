// Theme Switcher JavaScript
class ThemeSwitcher {
    constructor() {
        this.init();
    }

    init() {
        // Load saved theme or default to 'default'
        const savedTheme = localStorage.getItem('selected-theme') || 'default';
        this.setTheme(savedTheme);
        
        // Update dropdown if it exists
        this.updateDropdownSelection(savedTheme);
        
        // Handle RTL/LTR direction based on language
        this.handleDirectionality();
    }

    setTheme(themeName) {
        // Set data-theme attribute on document element
        document.documentElement.setAttribute('data-theme', themeName);
        
        // Save theme preference
        localStorage.setItem('selected-theme', themeName);
        
        // Update dropdown selection
        this.updateDropdownSelection(themeName);
    }

    updateDropdownSelection(themeName) {
        const dropdown = document.getElementById('themeDropdown');
        if (dropdown) {
            dropdown.value = themeName;
        }
    }

    handleDirectionality() {
        // Check if the HTML lang attribute indicates Arabic
        const htmlLang = document.documentElement.getAttribute('lang') || '';
        const htmlDir = document.documentElement.getAttribute('dir') || '';
        
        // Apply RTL class to body if Arabic or dir is rtl
        if (htmlLang === 'ar' || htmlDir === 'rtl') {
            document.body.classList.add('rtl');
            document.documentElement.setAttribute('dir', 'rtl');
        } else {
            document.body.classList.remove('rtl');
            document.documentElement.setAttribute('dir', 'ltr');
        }
    }

    handleThemeChange(event) {
        const selectedTheme = event.target.value;
        this.setTheme(selectedTheme);
    }
}

// Initialize theme switcher when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    const themeSwitcher = new ThemeSwitcher();
    
    // Bind dropdown change event
    const dropdown = document.getElementById('themeDropdown');
    if (dropdown) {
        dropdown.addEventListener('change', function(event) {
            themeSwitcher.handleThemeChange(event);
        });
    }
});