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
     HERO PARTICLE CANVAS (enhanced)
  ══════════════════════════════════════ */
  const canvas = document.getElementById('particles');
  if (canvas) {
    const ctx = canvas.getContext('2d');
    let W, H, pts;
    let mouseX = -9999, mouseY = -9999;

    canvas.addEventListener('mousemove', e => {
      const r = canvas.getBoundingClientRect();
      mouseX = e.clientX - r.left;
      mouseY = e.clientY - r.top;
    });
    canvas.addEventListener('mouseleave', () => { mouseX = -9999; mouseY = -9999; });

    const resize = () => {
      W = canvas.width  = canvas.offsetWidth;
      H = canvas.height = canvas.offsetHeight;
    };

    const init = () => {
      pts = Array.from({ length: 90 }, () => ({
        x:    Math.random() * W,
        y:    Math.random() * H,
        r:    Math.random() * 2.2 + 0.4,
        dx:   (Math.random() - 0.5) * 0.42,
        dy:   (Math.random() - 0.5) * 0.42,
        a:    Math.random() * 0.5 + 0.15,
        cyan: Math.random() > 0.45,
      }));
    };

    const draw = () => {
      ctx.clearRect(0, 0, W, H);

      pts.forEach(p => {
        const mx = mouseX - p.x;
        const my = mouseY - p.y;
        const md = Math.hypot(mx, my);
        if (md < 130 && md > 0) {
          p.dx -= (mx / md) * 0.055;
          p.dy -= (my / md) * 0.055;
        }
        p.dx *= 0.992; p.dy *= 0.992;
        p.x = (p.x + p.dx + W) % W;
        p.y = (p.y + p.dy + H) % H;

        ctx.beginPath();
        ctx.arc(p.x, p.y, p.r, 0, Math.PI * 2);
        ctx.fillStyle = p.cyan
          ? `rgba(0,207,255,${p.a})`
          : `rgba(26,127,232,${p.a * 0.8})`;
        ctx.fill();
      });

      pts.forEach((a, i) => {
        for (let j = i + 1; j < pts.length; j++) {
          const b = pts[j];
          const d = Math.hypot(a.x - b.x, a.y - b.y);
          if (d < 105) {
            ctx.beginPath();
            ctx.moveTo(a.x, a.y);
            ctx.lineTo(b.x, b.y);
            ctx.strokeStyle = `rgba(26,127,232,${(1 - d / 105) * 0.16})`;
            ctx.lineWidth = 0.6;
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
     CHATBOT DEMO SEND BUTTON
  ══════════════════════════════════════ */
  document.querySelector('.chat-send')?.addEventListener('click', () => {
    document.getElementById('contact')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
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
     NEURAL NET ANIMATION (enhanced)
  ══════════════════════════════════════ */
  const nodes = document.querySelectorAll('.node');
  const conns = document.querySelectorAll('.connection');

  if (nodes.length) {
    // Pulse nodes in a wave pattern
    let nIdx = 0;
    setInterval(() => {
      const n = nodes[nIdx % nodes.length];
      n.style.filter = 'drop-shadow(0 0 10px #00cfff) drop-shadow(0 0 20px rgba(0,207,255,.5))';
      n.style.transition = 'filter .15s';
      setTimeout(() => { n.style.filter = ''; }, 600);
      nIdx++;
    }, 180);

    // Cascade connections
    let cIdx = 0;
    setInterval(() => {
      const c = conns[cIdx % conns.length];
      c.style.opacity   = '1';
      c.style.stroke    = '#00cfff';
      c.style.strokeWidth = '1.5';
      setTimeout(() => {
        c.style.opacity     = '0.2';
        c.style.stroke      = '#1a7fe8';
        c.style.strokeWidth = '1';
      }, 500);
      cIdx++;
    }, 120);
  }


  /* ══════════════════════════════════════
     3D CARD TILT
  ══════════════════════════════════════ */
  document.querySelectorAll('.service-card, .why-card, .marketing-card').forEach(card => {
    card.addEventListener('mousemove', e => {
      const r  = card.getBoundingClientRect();
      const x  = (e.clientX - r.left)  / r.width;
      const y  = (e.clientY - r.top)   / r.height;
      const tx = (y - 0.5) * 10;
      const ty = (x - 0.5) * -10;
      card.style.transform = `perspective(720px) rotateX(${tx}deg) rotateY(${ty}deg) translateZ(6px)`;
      card.style.setProperty('--mouse-x', `${x * 100}%`);
      card.style.setProperty('--mouse-y', `${y * 100}%`);
    });
    card.addEventListener('mouseleave', () => {
      card.style.transform = '';
    });
  });


  /* ══════════════════════════════════════
     HERO MOUSE PARALLAX
  ══════════════════════════════════════ */
  const floatItems = document.querySelectorAll(
    '.float-orb, .float-hex, .float-badge'
  );
  if (floatItems.length) {
    let ticking = false;
    document.addEventListener('mousemove', e => {
      if (ticking) return;
      ticking = true;
      requestAnimationFrame(() => {
        const cx = window.innerWidth  / 2;
        const cy = window.innerHeight / 2;
        const dx = (e.clientX - cx) / cx;
        const dy = (e.clientY - cy) / cy;
        floatItems.forEach((el, i) => {
          const depth = (i % 3 + 1) * 7;
          el.style.transform = `translate(${dx * depth}px, ${dy * depth}px)`;
        });
        ticking = false;
      });
    });
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
