/**
 * Tiki-like UI Enhancements
 * Handles animations, countdown timers, carousel, and interactive elements
 */

document.addEventListener('DOMContentLoaded', function () {
  // Initialize countdown timer
  initCountdownTimer();

  // Initialize carousel if present
  initBannerCarousel();

  // Add smooth scroll behavior
  document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function (e) {
      e.preventDefault();
      const target = document.querySelector(this.getAttribute('href'));
      if (target) {
        target.scrollIntoView({ behavior: 'smooth' });
      }
    });
  });

  // Lazy load images
  if ('IntersectionObserver' in window) {
    const imageObserver = new IntersectionObserver((entries, observer) => {
      entries.forEach(entry => {
        if (entry.isIntersecting) {
          const img = entry.target;
          img.src = img.dataset.src || img.src;
          img.classList.remove('lazy');
          observer.unobserve(img);
        }
      });
    });

    document.querySelectorAll('img[data-src]').forEach(img => {
      imageObserver.observe(img);
    });
  }
});

/**
 * Countdown Timer for Flash Deals
 */
function initCountdownTimer() {
  const timerElements = {
    hours: document.getElementById('hours'),
    minutes: document.getElementById('minutes'),
    seconds: document.getElementById('seconds')
  };

  // Check if timer elements exist
  if (!timerElements.hours || !timerElements.minutes || !timerElements.seconds) {
    return;
  }

  // Set flash deal duration (2.5 hours in this example)
  const flashDealDuration = 2.5 * 60 * 60 * 1000; // 2.5 hours in milliseconds
  let timeRemaining = flashDealDuration;

  function updateTimer() {
    const hours = Math.floor(timeRemaining / (1000 * 60 * 60));
    const minutes = Math.floor((timeRemaining % (1000 * 60 * 60)) / (1000 * 60));
    const seconds = Math.floor((timeRemaining % (1000 * 60)) / 1000);

    // Update DOM with zero-padded values
    timerElements.hours.textContent = String(hours).padStart(2, '0');
    timerElements.minutes.textContent = String(minutes).padStart(2, '0');
    timerElements.seconds.textContent = String(seconds).padStart(2, '0');

    // Decrease time remaining
    if (timeRemaining > 0) {
      timeRemaining -= 1000;
    } else {
      // Reset timer when it reaches 0
      timeRemaining = flashDealDuration;
    }
  }

  // Update immediately and then every second
  updateTimer();
  setInterval(updateTimer, 1000);
}

/**
 * Banner Carousel
 */
function initBannerCarousel() {
  const adSubCards = document.querySelectorAll('.ad-sub-card');

  if (adSubCards.length === 0) {
    return;
  }

  let currentIndex = 0;

  // Add carousel indicators if multiple banners
  if (adSubCards.length > 1) {
    // Optional: Add carousel controls here
    // For now, we'll just auto-rotate
  }

  function rotateBanner() {
    adSubCards.forEach((card, index) => {
      if (index === currentIndex) {
        card.style.opacity = '1';
        card.style.transform = 'scale(1)';
        card.style.transition = 'all 0.5s ease';
      }
    });

    currentIndex = (currentIndex + 1) % adSubCards.length;
  }

  // Auto-rotate banners every 5 seconds
  if (adSubCards.length > 1) {
    setInterval(rotateBanner, 5000);
  }
}

/**
 * Product Card Interactions
 */
document.addEventListener('click', function (e) {
  const productCard = e.target.closest('.product-card');

  if (productCard && !e.target.closest('.buy-btn')) {
    const url = productCard.getAttribute('data-product-url');
    if (url) {
      window.open(url, '_blank', 'noopener');
    }
  }
});

/**
 * Search Input Focus Effect
 */
const searchInput = document.getElementById('headerSearch');
if (searchInput) {
  searchInput.addEventListener('focus', function () {
    this.closest('.header-search').classList.add('search-focused');
  });

  searchInput.addEventListener('blur', function () {
    this.closest('.header-search').classList.remove('search-focused');
  });
}

/**
 * Smooth Page Transitions
 */
window.addEventListener('beforeunload', function () {
  document.body.style.opacity = '0.8';
  document.body.style.transition = 'opacity 0.3s ease';
});

/**
 * Add scroll animations for product cards
 */
function observeProductCards() {
  if ('IntersectionObserver' in window) {
    const cardObserver = new IntersectionObserver((entries) => {
      entries.forEach((entry, index) => {
        if (entry.isIntersecting) {
          entry.target.style.animation = `fadeInUp 0.5s ease-out ${index * 0.05}s both`;
          cardObserver.unobserve(entry.target);
        }
      });
    }, {
      threshold: 0.1,
      rootMargin: '0px 0px -50px 0px'
    });

    document.querySelectorAll('.product-card').forEach(card => {
      cardObserver.observe(card);
    });
  }
}

// Call after products are loaded
document.addEventListener('DOMContentLoaded', observeProductCards);

// Re-observe when products are dynamically loaded
const originalFetch = window.fetch;
window.fetch = function (...args) {
  return originalFetch.apply(this, args).then(response => {
    if (response.ok) {
      // Wait a bit for DOM to update
      setTimeout(observeProductCards, 100);
    }
    return response;
  });
};

/**
 * Add CSS animations if not already in stylesheet
 */
const style = document.createElement('style');
style.textContent = `
  @keyframes fadeInUp {
    from {
      opacity: 0;
      transform: translateY(20px);
    }
    to {
      opacity: 1;
      transform: translateY(0);
    }
  }

  .search-focused .search-input {
    border-color: var(--tiki-blue);
    box-shadow: 0 0 0 3px rgba(26, 148, 255, 0.1);
  }

  .header-search {
    transition: all 0.3s ease;
  }

  .header-search.search-focused {
    transform: scale(1.02);
  }
`;
document.head.appendChild(style);
