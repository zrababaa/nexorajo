/* ========================================
   NEXORA — Main JavaScript
   nexorajo.com
   ======================================== */

document.addEventListener('DOMContentLoaded', () => {

  /* ══════════════════════════════════════
     LANGUAGE SWITCHER  (EN / AR + RTL)
  ══════════════════════════════════════ */
  const HTML  = document.documentElement;
  const STORE = localStorage;

  function applyLang(lang) {
    const isAr = lang === 'ar';

    // Direction + lang attribute
    HTML.setAttribute('lang', lang);
    HTML.setAttribute('dir',  isAr ? 'rtl' : 'ltr');

    // Arabic font injection
    if (isAr) {
      document.body.style.fontFamily = "'Cairo', 'Tajawal', 'Inter', sans-serif";
    } else {
      document.body.style.fontFamily = "'Inter', -apple-system, sans-serif";
    }

    // Swap all [data-en] / [data-ar] text nodes
    document.querySelectorAll('[data-en]').forEach(el => {
      const val = el.getAttribute(isAr ? 'data-ar' : 'data-en');
      if (val) {
        // For inputs/textarea swap placeholder, for others swap text
        if (el.tagName === 'INPUT' || el.tagName === 'TEXTAREA') {
          el.placeholder = val;
        } else {
          el.textContent = val;
        }
      }
    });

    // Swap data-placeholder-en / data-placeholder-ar on form elements
    document.querySelectorAll('[data-placeholder-en]').forEach(el => {
      const key = isAr ? 'data-placeholder-ar' : 'data-placeholder-en';
      el.placeholder = el.getAttribute(key) || '';
    });

    // Swap <option> text inside <select>
    document.querySelectorAll('option[data-en]').forEach(opt => {
      const val = opt.getAttribute(isAr ? 'data-ar' : 'data-en');
      if (val) opt.textContent = val;
    });

    // Toggle active state on all lang buttons
    document.querySelectorAll('.lang-btn').forEach(btn => {
      btn.classList.toggle('active', btn.dataset.lang === lang);
    });

    STORE.setItem('nexora-lang', lang);
  }

  // Attach click handlers to ALL language buttons (desktop + mobile)
  document.querySelectorAll('.lang-btn').forEach(btn => {
    btn.addEventListener('click', () => applyLang(btn.dataset.lang));
  });

  // Init from saved preference or browser language
  const saved   = STORE.getItem('nexora-lang');
  const browser = navigator.language?.startsWith('ar') ? 'ar' : 'en';
  applyLang(saved || browser);


  /* ══════════════════════════════════════
     NAVBAR SCROLL
  ══════════════════════════════════════ */
  const navbar = document.getElementById('navbar');
  window.addEventListener('scroll', () => {
    navbar.classList.toggle('scrolled', window.scrollY > 30);
  }, { passive: true });


  /* ══════════════════════════════════════
     MOBILE MENU
  ══════════════════════════════════════ */
  const hamburger  = document.getElementById('hamburgerBtn');
  const mobileMenu = document.getElementById('mobileMenu');
  const mobileClose= document.getElementById('mobileClose');

  hamburger?.addEventListener('click', () => mobileMenu.classList.add('open'));
  mobileClose?.addEventListener('click', () => mobileMenu.classList.remove('open'));
  mobileMenu?.querySelectorAll('a').forEach(a =>
    a.addEventListener('click', () => mobileMenu.classList.remove('open'))
  );


  /* ══════════════════════════════════════
     SCROLL REVEAL
  ══════════════════════════════════════ */
  const revealObs = new IntersectionObserver(
    entries => entries.forEach(e => {
      if (e.isIntersecting) {
        e.target.classList.add('visible');
        revealObs.unobserve(e.target);
      }
    }),
    { threshold: 0.1, rootMargin: '0px 0px -40px 0px' }
  );
  document.querySelectorAll('.reveal').forEach(el => revealObs.observe(el));


  /* ══════════════════════════════════════
     COUNTER ANIMATION
  ══════════════════════════════════════ */
  function animateCounter(el, target, suffix) {
    const dur = 2000;
    const t0  = performance.now();
    const tick = now => {
      const p    = Math.min((now - t0) / dur, 1);
      const ease = 1 - Math.pow(1 - p, 3);
      el.textContent = Math.floor(ease * target) + suffix;
      if (p < 1) requestAnimationFrame(tick);
    };
    requestAnimationFrame(tick);
  }

  const counterObs = new IntersectionObserver(entries => {
    entries.forEach(e => {
      if (e.isIntersecting) {
        animateCounter(e.target, +e.target.dataset.target, e.target.dataset.suffix || '');
        counterObs.unobserve(e.target);
      }
    });
  }, { threshold: 0.5 });
  document.querySelectorAll('[data-target]').forEach(el => counterObs.observe(el));


  /* ══════════════════════════════════════
     HERO PARTICLE CANVAS
  ══════════════════════════════════════ */
  const canvas = document.getElementById('particles');
  if (canvas) {
    const ctx = canvas.getContext('2d');
    let W, H, pts;

    const resize = () => {
      W = canvas.width  = canvas.offsetWidth;
      H = canvas.height = canvas.offsetHeight;
    };

    const init = () => {
      pts = Array.from({ length: 55 }, () => ({
        x: Math.random() * W,
        y: Math.random() * H,
        r: Math.random() * 1.8 + 0.4,
        dx: (Math.random() - 0.5) * 0.35,
        dy: (Math.random() - 0.5) * 0.35,
        a: Math.random() * 0.45 + 0.1,
      }));
    };

    const draw = () => {
      ctx.clearRect(0, 0, W, H);
      pts.forEach(p => {
        p.x = (p.x + p.dx + W) % W;
        p.y = (p.y + p.dy + H) % H;
        ctx.beginPath();
        ctx.arc(p.x, p.y, p.r, 0, Math.PI * 2);
        ctx.fillStyle = `rgba(0,207,255,${p.a})`;
        ctx.fill();
      });
      pts.forEach((a, i) => {
        for (let j = i + 1; j < pts.length; j++) {
          const b = pts[j];
          const d = Math.hypot(a.x - b.x, a.y - b.y);
          if (d < 90) {
            ctx.beginPath();
            ctx.moveTo(a.x, a.y);
            ctx.lineTo(b.x, b.y);
            ctx.strokeStyle = `rgba(26,127,232,${(1 - d / 90) * 0.12})`;
            ctx.lineWidth = 0.5;
            ctx.stroke();
          }
        }
      });
      requestAnimationFrame(draw);
    };

    resize(); init(); draw();
    window.addEventListener('resize', () => { resize(); init(); });
  }


  /* ══════════════════════════════════════
     SMOOTH SCROLL
  ══════════════════════════════════════ */
  document.querySelectorAll('a[href^="#"]').forEach(a => {
    a.addEventListener('click', e => {
      const tgt = document.querySelector(a.getAttribute('href'));
      if (tgt) { e.preventDefault(); tgt.scrollIntoView({ behavior: 'smooth', block: 'start' }); }
    });
  });


  /* ══════════════════════════════════════
     CONTACT FORM
  ══════════════════════════════════════ */
  const form = document.getElementById('contactForm');
  form?.addEventListener('submit', async e => {
    e.preventDefault();
    const btn = form.querySelector('[type="submit"]');
    const orig = btn.textContent;
    const isAr = HTML.getAttribute('lang') === 'ar';
    btn.textContent = isAr ? 'جارٍ الإرسال...' : 'Sending...';
    btn.disabled = true;
    await new Promise(r => setTimeout(r, 1400));
    btn.textContent = isAr ? 'تم الإرسال! ✓' : 'Message Sent! ✓';
    btn.style.background = 'linear-gradient(135deg,#22c55e,#16a34a)';
    setTimeout(() => {
      btn.textContent    = orig;
      btn.style.background = '';
      btn.disabled = false;
      form.reset();
    }, 3000);
  });


  /* ══════════════════════════════════════
     NEURAL NET ANIMATION
  ══════════════════════════════════════ */
  const nodes = document.querySelectorAll('.node');
  const conns = document.querySelectorAll('.connection');

  if (nodes.length) {
    setInterval(() => {
      const n = nodes[Math.floor(Math.random() * nodes.length)];
      n.style.filter = 'drop-shadow(0 0 8px #00cfff)';
      setTimeout(() => n.style.filter = '', 500);
    }, 700);

    setInterval(() => {
      const c = conns[Math.floor(Math.random() * conns.length)];
      c.style.opacity = '0.85';
      c.style.stroke  = '#00cfff';
      setTimeout(() => { c.style.opacity = '0.2'; c.style.stroke = '#1a7fe8'; }, 450);
    }, 350);
  }


  /* ══════════════════════════════════════
     ACTIVE NAV HIGHLIGHT
  ══════════════════════════════════════ */
  const sections = document.querySelectorAll('section[id]');
  const navLinks = document.querySelectorAll('.nav-links a');

  new IntersectionObserver(entries => {
    entries.forEach(e => {
      if (e.isIntersecting) {
        navLinks.forEach(l => l.classList.remove('active'));
        document.querySelector(`.nav-links a[href="#${e.target.id}"]`)?.classList.add('active');
      }
    });
  }, { threshold: 0.35 }).forEach
    ? null
    : void 0;

  const secObs = new IntersectionObserver(entries => {
    entries.forEach(e => {
      if (e.isIntersecting) {
        navLinks.forEach(l => l.classList.remove('active'));
        document.querySelector(`.nav-links a[href="#${e.target.id}"]`)?.classList.add('active');
      }
    });
  }, { threshold: 0.35 });
  sections.forEach(s => secObs.observe(s));

});
