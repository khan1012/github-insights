// ==========================================
// DOM Helper Functions
// Handles all DOM manipulation
// ==========================================

const DOMHelper = {
    /**
     * Get element by ID
     */
    getElement(id) {
        return document.getElementById(id);
    },

    /**
     * Update text content of an element
     */
    updateText(elementId, text) {
        const element = this.getElement(elementId);
        if (element) {
            element.textContent = text;
        }
    },

    /**
     * Add loading state to stat card
     */
    addLoadingState(elementId) {
        const element = this.getElement(elementId);
        if (element) {
            const card = element.closest('.stat-card');
            if (card) {
                card.classList.add('loading');
            }
        }
    },

    /**
     * Remove loading state from stat card
     */
    removeLoadingState(elementId) {
        const element = this.getElement(elementId);
        if (element) {
            const card = element.closest('.stat-card');
            if (card) {
                card.classList.remove('loading');
            }
        }
    },

    /**
     * Show element
     */
    show(elementId) {
        const element = this.getElement(elementId);
        if (element) {
            element.classList.remove('hidden');
        }
    },

    /**
     * Hide element
     */
    hide(elementId) {
        const element = this.getElement(elementId);
        if (element) {
            element.classList.add('hidden');
        }
    },

    /**
     * Set HTML content
     */
    setHTML(elementId, html) {
        const element = this.getElement(elementId);
        if (element) {
            element.innerHTML = html;
        }
    },

    /**
     * Add CSS class
     */
    addClass(elementId, className) {
        const element = this.getElement(elementId);
        if (element) {
            element.classList.add(className);
        }
    }
};
