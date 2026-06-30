/* ========================================
   NEXORA — Main JavaScript
   nexorajo.com
   ======================================== */

document.addEventListener('DOMContentLoaded', () => {

  /* ── NAVBAR SCROLL ── */
  const navbar = document.getElementById('navbar');
  const onScroll = () => {
    navbar.classList.toggle('scrolled', window.scrollY > 40);
  };
  window.addEventListener('scroll', onScroll, { passive: true });

  /* ── MOBILE MENU ── */
  const hamburger = document.querySelector('.hamburger');
  const mobileMenu = document.querySelector('.mobile-menu');
  const mobileClose = document.querySelector('.mobile-close');

  hamburger?.addEventListener('click', () => mobileMenu?.classList.add('open'));
  mobileClose?.addEventListener('click', () => mobileMenu?.classList.remove('open'));
  mobileMenu?.querySelectorAll('a').forEach(a =>
    a.addEventListener('click', () => mobileMenu.classList.remove('open'))
  );

  /* ── SCROLL REVEAL ── */
  const revealObserver = new IntersectionObserver(
    (entries) => {
      entries.forEach(e => {
        if (e.isIntersecting) {
          e.target.classList.add('visible');
          revealObserver.unobserve(e.target);
        }
      });
    },
    { threshold: 0.12, rootMargin: '0px 0px -40px 0px' }
  );
  document.querySelectorAll('.reveal').forEach(el => revealObserver.observe(el));

  /* ── COUNTER ANIMATION ── */
  const animateCounter = (el, target, suffix = '') => {
    const duration = 2000;
    const start = performance.now();
    const update = (now) => {
      const t = Math.min((now - start) / duration, 1);
      const ease = 1 - Math.pow(1 - t, 3);
      el.textContent = Math.floor(ease * target) + suffix;
      if (t < 1) requestAnimationFrame(update);
    };
    requestAnimationFrame(update);
  };

  const counterObserver = new IntersectionObserver(
    (entries) => {
      entries.forEach(e => {
        if (e.isIntersecting) {
          const el = e.target;
          const target = parseInt(el.dataset.target);
          const suffix = el.dataset.suffix || '';
          animateCounter(el, target, suffix);
          counterObserver.unobserve(el);
        }
      });
    },
    { threshold: 0.5 }
  );
  document.querySelectorAll('[data-target]').forEach(el => counterObserver.observe(el));

  /* ── HERO PARTICLES ── */
  const canvas = document.getElementById('particles');
  if (canvas) {
    const ctx = canvas.getContext('2d');
    let W, H, particles;

    const resize = () => {
      W = canvas.width = canvas.offsetWidth;
      H = canvas.height = canvas.offsetHeight;
    };

    const initParticles = () => {
      particles = Array.from({ length: 60 }, () => ({
        x: Math.random() * W,
        y: Math.random() * H,
        r: Math.random() * 2 + 0.5,
        dx: (Math.random() - 0.5) * 0.4,
        dy: (Math.random() - 0.5) * 0.4,
        alpha: Math.random() * 0.5 + 0.1,
      }));
    };

    const draw = () => {
      ctx.clearRect(0, 0, W, H);
      particles.forEach(p => {
        p.x += p.dx;
        p.y += p.dy;
        if (p.x < 0 || p.x > W) p.dx *= -1;
        if (p.y < 0 || p.y > H) p.dy *= -1;

        ctx.beginPath();
        ctx.arc(p.x, p.y, p.r, 0, Math.PI * 2);
        ctx.fillStyle = `rgba(0, 207, 255, ${p.alpha})`;
        ctx.fill();
      });

      // Draw connecting lines
      particles.forEach((a, i) => {
        particles.slice(i + 1).forEach(b => {
          const dist = Math.hypot(a.x - b.x, a.y - b.y);
          if (dist < 100) {
            ctx.beginPath();
            ctx.moveTo(a.x, a.y);
            ctx.lineTo(b.x, b.y);
            ctx.strokeStyle = `rgba(26, 127, 232, ${(1 - dist / 100) * 0.15})`;
            ctx.lineWidth = 0.5;
            ctx.stroke();
          }
        });
      });

      requestAnimationFrame(draw);
    };

    resize();
    initParticles();
    draw();
    window.addEventListener('resize', () => { resize(); initParticles(); });
  }

  /* ── SMOOTH SCROLL ── */
  document.querySelectorAll('a[href^="#"]').forEach(a => {
    a.addEventListener('click', e => {
      const target = document.querySelector(a.getAttribute('href'));
      if (target) {
        e.preventDefault();
        target.scrollIntoView({ behavior: 'smooth', block: 'start' });
      }
    });
  });

  /* ── CONTACT FORM ── */
  const form = document.getElementById('contactForm');
  form?.addEventListener('submit', async (e) => {
    e.preventDefault();
    const btn = form.querySelector('[type="submit"]');
    const original = btn.textContent;
    btn.textContent = 'Sending...';
    btn.disabled = true;

    // Simulate sending (replace with real API call)
    await new Promise(r => setTimeout(r, 1500));
    btn.textContent = 'Message Sent!';
    btn.style.background = 'linear-gradient(135deg, #22c55e, #16a34a)';

    setTimeout(() => {
      btn.textContent = original;
      btn.style.background = '';
      btn.disabled = false;
      form.reset();
    }, 3000);
  });

  /* ── NEURAL NET SVG ANIMATION ── */
  const svg = document.querySelector('.neural-svg');
  if (svg) {
    const nodes = svg.querySelectorAll('.node');
    const connections = svg.querySelectorAll('.connection');

    setInterval(() => {
      const randomNode = nodes[Math.floor(Math.random() * nodes.length)];
      randomNode.style.filter = 'drop-shadow(0 0 8px #00cfff)';
      setTimeout(() => randomNode.style.filter = '', 600);
    }, 800);

    setInterval(() => {
      const randomConn = connections[Math.floor(Math.random() * connections.length)];
      randomConn.style.opacity = '0.9';
      randomConn.style.stroke = '#00cfff';
      setTimeout(() => {
        randomConn.style.opacity = '0.2';
        randomConn.style.stroke = '#1a7fe8';
      }, 500);
    }, 400);
  }

  /* ── CHAT ANIMATION ── */
  const chatBody = document.querySelector('.chat-body');
  if (chatBody) {
    const messages = chatBody.querySelectorAll('.chat-msg');
    messages.forEach((msg, i) => {
      msg.style.opacity = '0';
      msg.style.animationDelay = `${i * 0.6 + 0.5}s`;
      msg.style.animationFillMode = 'both';
      msg.style.animationName = 'fadeUp';
      msg.style.animationDuration = '0.5s';
      msg.style.animationTimingFunction = 'ease';
    });

    // Show typing indicator briefly
    const typingObserver = new IntersectionObserver(entries => {
      if (entries[0].isIntersecting) {
        messages.forEach(m => m.style.opacity = '');
        typingObserver.disconnect();
      }
    }, { threshold: 0.5 });
    typingObserver.observe(chatBody);
  }

  /* ── ACTIVE NAV HIGHLIGHT ── */
  const sections = document.querySelectorAll('section[id]');
  const navLinks = document.querySelectorAll('.nav-links a');

  const sectionObserver = new IntersectionObserver(
    (entries) => {
      entries.forEach(e => {
        if (e.isIntersecting) {
          navLinks.forEach(l => l.classList.remove('active'));
          const active = document.querySelector(`.nav-links a[href="#${e.target.id}"]`);
          active?.classList.add('active');
        }
      });
    },
    { threshold: 0.4 }
  );
  sections.forEach(s => sectionObserver.observe(s));

});
