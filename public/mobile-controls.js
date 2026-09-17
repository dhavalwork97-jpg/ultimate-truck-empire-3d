(() => {
  const isTouch = matchMedia('(pointer: coarse)').matches || 'ontouchstart' in window || navigator.maxTouchPoints > 0;
  if (!isTouch) return;

  const style = document.createElement('style');
  style.textContent = `
    #ute-mobile-controls{position:fixed;inset:0;z-index:20;pointer-events:none;font-family:Inter,system-ui,sans-serif;user-select:none;touch-action:none}
    #ute-mobile-controls .cluster{position:absolute;bottom:max(18px,env(safe-area-inset-bottom));display:flex;gap:12px;align-items:end;pointer-events:auto}
    #ute-mobile-controls .left{left:max(16px,env(safe-area-inset-left));}
    #ute-mobile-controls .right{right:max(16px,env(safe-area-inset-right));}
    #ute-mobile-controls .joystick{width:132px;height:132px;border-radius:50%;background:rgba(8,14,21,.72);border:1px solid rgba(255,255,255,.18);box-shadow:0 12px 35px rgba(0,0,0,.35),inset 0 0 0 18px rgba(255,255,255,.025);position:relative;backdrop-filter:blur(10px)}
    #ute-mobile-controls .stick{position:absolute;left:50%;top:50%;width:58px;height:58px;margin:-29px;border-radius:50%;background:rgba(232,93,63,.92);border:2px solid rgba(255,255,255,.65);box-shadow:0 6px 18px rgba(0,0,0,.35)}
    #ute-mobile-controls .pedals{display:flex;flex-direction:column;gap:10px}
    #ute-mobile-controls button{width:74px;height:58px;border-radius:16px;border:1px solid rgba(255,255,255,.16);background:rgba(8,14,21,.78);color:#fff;font-weight:800;font-size:10px;letter-spacing:1px;box-shadow:0 10px 28px rgba(0,0,0,.35);backdrop-filter:blur(10px);touch-action:none}
    #ute-mobile-controls button:active{transform:scale(.96);background:rgba(232,93,63,.82)}
    #ute-mobile-controls .hint{position:absolute;left:50%;bottom:18px;transform:translateX(-50%);padding:7px 10px;border-radius:999px;background:rgba(6,11,17,.65);border:1px solid rgba(255,255,255,.1);color:rgba(255,255,255,.62);font-size:8px;letter-spacing:1px;white-space:nowrap;pointer-events:none}
    @media(min-width:701px){#ute-mobile-controls{display:none}}
  `;
  document.head.appendChild(style);

  const root = document.createElement('div');
  root.id = 'ute-mobile-controls';
  root.innerHTML = `
    <div class="cluster left"><div class="joystick" aria-label="Steering joystick"><div class="stick"></div></div></div>
    <div class="cluster right"><div class="pedals"><button data-key="Space">BRAKE</button><button data-key="KeyW">THROTTLE</button></div></div>
    <div class="hint">STEER • THROTTLE • BRAKE</div>
  `;
  document.body.appendChild(root);

  const pressed = new Set();
  const send = (code, down) => {
    if (down && !pressed.has(code)) { pressed.add(code); window.dispatchEvent(new KeyboardEvent('keydown', {code, key: code === 'Space' ? ' ' : code.replace('Key','').toLowerCase(), bubbles:true})); }
    if (!down && pressed.has(code)) { pressed.delete(code); window.dispatchEvent(new KeyboardEvent('keyup', {code, key: code === 'Space' ? ' ' : code.replace('Key','').toLowerCase(), bubbles:true})); }
  };

  root.querySelectorAll('button[data-key]').forEach(btn => {
    const code = btn.dataset.key;
    const start = e => { e.preventDefault(); btn.setPointerCapture?.(e.pointerId); send(code,true); };
    const stop = e => { e.preventDefault(); send(code,false); };
    btn.addEventListener('pointerdown',start); btn.addEventListener('pointerup',stop); btn.addEventListener('pointercancel',stop); btn.addEventListener('pointerleave',stop);
  });

  const joy = root.querySelector('.joystick');
  const stick = root.querySelector('.stick');
  let joyId = null;
  const updateJoy = (x,y) => {
    const r = joy.getBoundingClientRect(), cx=r.left+r.width/2, cy=r.top+r.height/2;
    let dx=x-cx, dy=y-cy, len=Math.hypot(dx,dy), max=r.width*.31;
    if(len>max){dx*=max/len;dy*=max/len;}
    stick.style.transform=`translate(${dx}px,${dy}px)`;
    const nx=dx/max, ny=dy/max;
    const dead=.22;
    send('KeyA',nx < -dead); send('KeyD',nx > dead);
    // Forward/back is mapped to vertical joystick travel. Small steering input can still start the truck.
    send('KeyW',ny < -dead); send('KeyS',ny > dead);
  };
  const resetJoy = () => { joyId=null; stick.style.transform='translate(0,0)'; ['KeyA','KeyD','KeyW','KeyS'].forEach(k=>send(k,false)); };
  joy.addEventListener('pointerdown',e=>{e.preventDefault();joyId=e.pointerId;joy.setPointerCapture?.(e.pointerId);updateJoy(e.clientX,e.clientY);});
  joy.addEventListener('pointermove',e=>{if(e.pointerId===joyId){e.preventDefault();updateJoy(e.clientX,e.clientY);}});
  joy.addEventListener('pointerup',e=>{if(e.pointerId===joyId)resetJoy();}); joy.addEventListener('pointercancel',resetJoy);

  // Desktop steering fix: the existing driving loop only rotates while the truck is moving.
  // Give a stationary A/D input a tiny forward impulse so the first steering input is responsive.
  let steering = false;
  window.addEventListener('keydown', e => {
    if ((e.code === 'KeyA' || e.code === 'KeyD') && !steering) {
      steering = true;
      if (!e.repeat) window.dispatchEvent(new KeyboardEvent('keydown',{code:'KeyW',key:'w',bubbles:true}));
    }
  }, true);
  window.addEventListener('keyup', e => {
    if (e.code === 'KeyA' || e.code === 'KeyD') {
      setTimeout(() => { steering=false; if(!pressed.has('KeyW')) window.dispatchEvent(new KeyboardEvent('keyup',{code:'KeyW',key:'w',bubbles:true})); }, 80);
    }
  }, true);
})();
