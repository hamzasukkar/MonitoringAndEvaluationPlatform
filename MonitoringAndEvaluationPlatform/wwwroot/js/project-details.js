// Project Details Optimized JavaScript

class ProjectDetailsManager {
    constructor() {
        this.init();
    }

    init() {
        // Initialize progress circles immediately for better UX
        this.initializeProgressCircles();

        // Initialize page animations
        this.initializeAnimations();

        // Initialize file deletion functionality
        this.initializeFileActions();
    }

    initializeProgressCircles() {
        const circles = document.querySelectorAll('.progress-circle');
        circles.forEach(circle => {
            const value = circle.getAttribute('data-value');
            circle.style.setProperty('--value', value);

            // Apply color class based on value
            if (value <= 50) {
                circle.classList.add('progress-0-50');
            } else if (value <= 75) {
                circle.classList.add('progress-51-75');
            } else {
                circle.classList.add('progress-76-100');
            }
        });
    }

    initializeAnimations() {
        // Use Intersection Observer for better performance
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    this.animateElement(entry.target);
                    observer.unobserve(entry.target);
                }
            });
        }, { threshold: 0.1 });

        // Observe info cards for animation
        document.querySelectorAll('.info-card').forEach(card => {
            observer.observe(card);
        });

        // Animate progress circles when visible
        document.querySelectorAll('.progress-circle').forEach(circle => {
            observer.observe(circle);
        });
    }

    animateElement(element) {
        if (element.classList.contains('info-card')) {
            element.style.opacity = '0';
            element.style.transform = 'translateY(20px)';
            element.style.transition = 'all 0.6s ease';

            // Use requestAnimationFrame for smooth animation
            requestAnimationFrame(() => {
                element.style.opacity = '1';
                element.style.transform = 'translateY(0)';
            });
        } else if (element.classList.contains('progress-circle')) {
            // Animate progress circle
            setTimeout(() => {
                element.style.transform = 'scale(1.05)';
                setTimeout(() => {
                    element.style.transform = 'scale(1)';
                }, 200);
            }, 100);
        }
    }

    initializeFileActions() {
        // Use event delegation for better performance
        document.addEventListener('click', (e) => {
            if (e.target.closest('[onclick*="deleteFile"]')) {
                e.preventDefault();

                // Extract file info from the onclick attribute
                const button = e.target.closest('button');
                const onclickAttr = button.getAttribute('onclick');
                const matches = onclickAttr.match(/deleteFile\((\d+),\s*'([^']+)'\)/);

                if (matches) {
                    const fileId = matches[1];
                    const fileName = matches[2];
                    this.deleteFile(fileId, fileName);
                }
            }
        });
    }

    deleteFile(fileId, fileName) {
        // Use modern SweetAlert2 with optimized config
        Swal.fire({
            title: window.deleteFileStrings?.title || 'Are you sure?',
            text: `${window.deleteFileStrings?.text || 'This will delete the file'}: ${fileName}`,
            icon: 'warning',
            showCancelButton: true,
            cancelButtonText: window.deleteFileStrings?.cancel || 'Cancel',
            confirmButtonText: window.deleteFileStrings?.confirm || 'Yes, delete it!',
            reverseButtons: true,
            confirmButtonColor: '#dc3545',
            cancelButtonColor: '#6c757d',
            customClass: {
                popup: 'modern-swal-popup',
                title: 'modern-swal-title'
            }
        }).then((result) => {
            if (result.isConfirmed) {
                this.submitDeleteForm(fileId);
            }
        });
    }

    submitDeleteForm(fileId) {
        const form = document.createElement('form');
        form.method = 'POST';
        form.action = `/Projects/DeleteFile/${fileId}`;

        // Add anti-forgery token if available
        const token = document.querySelector('input[name="__RequestVerificationToken"]');
        if (token) {
            const tokenInput = document.createElement('input');
            tokenInput.type = 'hidden';
            tokenInput.name = '__RequestVerificationToken';
            tokenInput.value = token.value;
            form.appendChild(tokenInput);
        }

        document.body.appendChild(form);
        form.submit();
    }
}

// Initialize when DOM is ready - optimized approach
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => new ProjectDetailsManager());
} else {
    new ProjectDetailsManager();
}