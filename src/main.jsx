import React, { useEffect, useMemo, useRef, useState } from 'react';
import { createRoot } from 'react-dom/client';
import * as THREE from 'three';
import './styles.css';

const SAVE_KEY = 'ute3d-save-v1';
const clamp = (n, a, b) => Math.max(a, Math.min(b, n));
const money = (n) => `$${Math.round(n).toLocaleString('en-US')}`;

const contracts = [
  { id: 'retail', title: 'Retail Restock', from: 'Metro Hub', to: 'North Point', pay: 1450, distance: 420, xp: 120 },
  { id: 'steel', title: 'Steel Run', from: 'Industrial Port', to: 'Ironworks', pay: 2200, distance: 610, xp: 180 },
  { id: 'cold', title: 'Cold Chain', from: 'Fresh Foods', to: 'Airport', pay: 3150, distance: 780, xp: 260 },
  { id: 'express', title: 'Express Freight', from: 'Metro Hub', to: 'Airport', pay: 4100, distance: 980, xp: 340 },
];

function loadGame() {
  try { return JSON.parse(localStorage.getItem(SAVE_KEY)) || null; } catch { return null; }
}

function Truck({ color = 0xe85d3f, scale = 1, trailer = true }) {
  const group = new THREE.Group();
  const cab = new THREE.Mesh(new THREE.BoxGeometry(2.2 * scale, 1.65 * scale, 3 * scale), new THREE.MeshStandardMaterial({ color, roughness: .72 }));
  cab.position.set(0, 1.25 * scale, 1.35 * scale); group.add(cab);
  const hood = new THREE.Mesh(new THREE.BoxGeometry(2.2 * scale, .7 * scale, .85 * scale), new THREE.MeshStandardMaterial({ color: 0x202833, roughness: .65 }));
  hood.position.set(0, .95 * scale, 2.95 * scale); group.add(hood);
  const glass = new THREE.Mesh(new THREE.BoxGeometry(1.7 * scale, .62 * scale, .08 * scale), new THREE.MeshStandardMaterial({ color: 0x8fc7d8, metalness: .1, roughness: .2 }));
  glass.position.set(0, 1.55 * scale, 2.89 * scale); group.add(glass);
  if (trailer) {
    const box = new THREE.Mesh(new THREE.BoxGeometry(2.45 * scale, 2.25 * scale, 4.7 * scale), new THREE.MeshStandardMaterial({ color: 0xdfe5e8, roughness: .8 }));
    box.position.set(0, 1.48 * scale, -2.1 * scale); group.add(box);
    const stripe = new THREE.Mesh(new THREE.BoxGeometry(2.48 * scale, .18 * scale, 4.72 * scale), new THREE.MeshStandardMaterial({ color: 0xe85d3f, roughness: .7 }));
    stripe.position.set(0, 1.12 * scale, -2.1 * scale); group.add(stripe);
  }
  const wheelMat = new THREE.MeshStandardMaterial({ color: 0x11151b, roughness: .9 });
  const hubMat = new THREE.MeshStandardMaterial({ color: 0xaeb8c2, metalness: .8, roughness: .25 });
  const positions = trailer ? [[-1.18,.65,2.15],[1.18,.65,2.15],[-1.18,.65,-.6],[1.18,.65,-.6],[-1.18,.65,-3.55],[1.18,.65,-3.55]] : [[-1.18,.65,2],[1.18,.65,2]];
  positions.forEach(([x,y,z]) => { const w = new THREE.Mesh(new THREE.CylinderGeometry(.48 * scale,.48 * scale,.32 * scale,20), wheelMat); w.rotation.z = Math.PI/2; w.position.set(x*scale,y*scale,z*scale); group.add(w); const h = new THREE.Mesh(new THREE.CylinderGeometry(.18*scale,.18*scale,.34*scale,16),hubMat); h.rotation.z=Math.PI/2; h.position.copy(w.position); group.add(h); });
  return group;
}

function World({ game, setGame, activeContract, setActiveContract }) {
  const mount = useRef(null);
  const stateRef = useRef({ keys: {}, speed: 0, angle: 0, pos: new THREE.Vector3(0,0,15) });
  const truckRef = useRef(null);
  const cameraRef = useRef(null);
  const [ready, setReady] = useState(false);

  useEffect(() => {
    const el = mount.current;
    const scene = new THREE.Scene(); scene.background = new THREE.Color(0x09111a); scene.fog = new THREE.Fog(0x09111a, 55, 180);
    const camera = new THREE.PerspectiveCamera(55, el.clientWidth / el.clientHeight, .1, 500); cameraRef.current = camera;
    const renderer = new THREE.WebGLRenderer({ antialias: true, powerPreference: 'high-performance' }); renderer.setPixelRatio(Math.min(devicePixelRatio, 2)); renderer.setSize(el.clientWidth, el.clientHeight); renderer.shadowMap.enabled = true; renderer.shadowMap.type = THREE.PCFSoftShadowMap; el.appendChild(renderer.domElement);
    const hemi = new THREE.HemisphereLight(0x9fc7ff, 0x182014, 1.7); scene.add(hemi);
    const sun = new THREE.DirectionalLight(0xffe0b0, 2.5); sun.position.set(-35,45,25); sun.castShadow=true; sun.shadow.mapSize.set(2048,2048); scene.add(sun);
    const ground = new THREE.Mesh(new THREE.PlaneGeometry(240,240), new THREE.MeshStandardMaterial({color:0x17231c,roughness:1})); ground.rotation.x=-Math.PI/2; ground.receiveShadow=true; scene.add(ground);
    const roadMat = new THREE.MeshStandardMaterial({color:0x252b31,roughness:.92});
    const road = new THREE.Mesh(new THREE.PlaneGeometry(18,220),roadMat); road.rotation.x=-Math.PI/2; road.position.y=.01; scene.add(road);
    const road2 = new THREE.Mesh(new THREE.PlaneGeometry(220,14),roadMat); road2.rotation.x=-Math.PI/2; road2.position.y=.012; scene.add(road2);
    const lineMat = new THREE.MeshBasicMaterial({color:0xf5d76e});
    for(let z=-100;z<110;z+=8){ const l=new THREE.Mesh(new THREE.PlaneGeometry(.25,4),lineMat); l.rotation.x=-Math.PI/2;l.position.set(0,.025,z);scene.add(l); }
    for(let x=-100;x<110;x+=8){ const l=new THREE.Mesh(new THREE.PlaneGeometry(4,.25),lineMat); l.rotation.x=-Math.PI/2;l.position.set(x,.026,0);scene.add(l); }
    // Depot platform and buildings
    const depot = new THREE.Mesh(new THREE.BoxGeometry(34,.25,30),new THREE.MeshStandardMaterial({color:0x303942,roughness:.85})); depot.position.set(-33,.15,18); depot.receiveShadow=true;scene.add(depot);
    const building = new THREE.Mesh(new THREE.BoxGeometry(24,7,11),new THREE.MeshStandardMaterial({color:0x48515a,roughness:.8})); building.position.set(-33,3.65,10);building.castShadow=true;scene.add(building);
    const roof = new THREE.Mesh(new THREE.BoxGeometry(25,.45,12),new THREE.MeshStandardMaterial({color:0x111820,metalness:.3,roughness:.5})); roof.position.set(-33,7.35,10);scene.add(roof);
    for(let i=-42;i<=-24;i+=6){ const door=new THREE.Mesh(new THREE.BoxGeometry(4.5,5.2,.12),new THREE.MeshStandardMaterial({color:0x101820,metalness:.2,roughness:.35}));door.position.set(i,2.8,4.45);scene.add(door); }
    // city blocks
    for(let i=0;i<30;i++){ const x=(i*37)%150-75,z=(i*53)%150-75; if(Math.abs(x)<13||Math.abs(z)<11) continue; const h=3+(i%7)*1.4; const b=new THREE.Mesh(new THREE.BoxGeometry(7+(i%3)*3,h,7+(i%4)*2),new THREE.MeshStandardMaterial({color:0x27333d,roughness:.9}));b.position.set(x,h/2,z);b.castShadow=true;scene.add(b); }
    // trees
    for(let i=0;i<55;i++){const x=(i*17)%210-105,z=(i*31)%210-105;if(Math.abs(x)<20&&Math.abs(z)<20)continue;const trunk=new THREE.Mesh(new THREE.CylinderGeometry(.18,.24,1.6,8),new THREE.MeshStandardMaterial({color:0x5c402d}));trunk.position.set(x,.8,z);scene.add(trunk);const crown=new THREE.Mesh(new THREE.IcosahedronGeometry(1.5+(i%3)*.4,1),new THREE.MeshStandardMaterial({color:0x31563a,roughness:1}));crown.position.set(x,2.4+(i%2)*.5,z);scene.add(crown);}
    const truck = Truck({color:0xe85d3f,scale:1}); truck.position.copy(stateRef.current.pos); truck.castShadow=true; truck.traverse(o=>{if(o.isMesh)o.castShadow=true});scene.add(truck);truckRef.current=truck;
    const parked = [Truck({color:0x3b82f6,scale:.78}),Truck({color:0x9aa3ad,scale:.78}),Truck({color:0x22a06b,scale:.78})]; parked[0].position.set(-27,.05,18);parked[0].rotation.y=Math.PI/2;parked[1].position.set(-35,.05,22);parked[1].rotation.y=Math.PI/2;parked[2].position.set(-43,.05,18);parked[2].rotation.y=Math.PI/2;parked.forEach(t=>{t.traverse(o=>{if(o.isMesh)o.castShadow=true});scene.add(t);});
    const keys=stateRef.current.keys; const down=e=>{keys[e.code]=true}; const up=e=>{keys[e.code]=false}; window.addEventListener('keydown',down);window.addEventListener('keyup',up);
    let raf; let last=performance.now();
    const loop=(now)=>{const dt=Math.min((now-last)/1000,.05);last=now; const s=stateRef.current; let throttle=(keys.KeyW||keys.ArrowUp)?1:0; let brake=(keys.KeyS||keys.ArrowDown)?1:0; let steer=(keys.KeyA||keys.ArrowLeft)?-1:((keys.KeyD||keys.ArrowRight)?1:0); s.speed += (throttle*15-brake*22-s.speed*2.2)*dt; s.speed=clamp(s.speed,-7,18); s.angle += steer*s.speed*.035*dt; const dir=new THREE.Vector3(Math.sin(s.angle),0,Math.cos(s.angle)); s.pos.addScaledVector(dir,s.speed*dt); s.pos.x=clamp(s.pos.x,-103,103);s.pos.z=clamp(s.pos.z,-103,103); if(truckRef.current){truckRef.current.position.copy(s.pos);truckRef.current.rotation.y=s.angle;}
      const target=s.pos.clone(); target.y=0; const camOffset=new THREE.Vector3(-Math.sin(s.angle)*10,7.5,-Math.cos(s.angle)*10); const desired=target.clone().add(camOffset); camera.position.lerp(desired,.08); camera.lookAt(target.clone().add(new THREE.Vector3(0,1.5,0))); renderer.render(scene,camera); raf=requestAnimationFrame(loop);}; raf=requestAnimationFrame(loop); setReady(true);
    const resize=()=>{camera.aspect=el.clientWidth/el.clientHeight;camera.updateProjectionMatrix();renderer.setSize(el.clientWidth,el.clientHeight)};window.addEventListener('resize',resize);
    return()=>{cancelAnimationFrame(raf);window.removeEventListener('resize',resize);window.removeEventListener('keydown',down);window.removeEventListener('keyup',up);renderer.dispose();el.removeChild(renderer.domElement)};
  },[]);
  useEffect(()=>{ if(activeContract && stateRef.current.pos.length()<5){ setActiveContract(null); } },[activeContract]);
  return <div className="world" ref={mount}>{!ready&&<div className="loading">INITIALIZING 3D WORLD…</div>}<div className="controls-hint"><b>WASD</b> / <b>ARROWS</b> Drive · Explore the depot & highways</div></div>
}

function App(){
  const saved=useMemo(loadGame,[]);
  const [game,setGame]=useState(saved||{money:25000,level:1,xp:0,trucks:1,drivers:0,garage:1,completed:0,active:null});
  const [tab,setTab]=useState('overview'); const [toast,setToast]=useState('');
  const xpNeed=100+game.level*80; const xpPct=clamp(game.xp/xpNeed*100,0,100);
  const save=(next)=>{setGame(next);try{localStorage.setItem(SAVE_KEY,JSON.stringify(next))}catch{}};
  const notify=(m)=>{setToast(m);setTimeout(()=>setToast(''),2600)};
  const takeContract=(c)=>{save({...game,active:c.id});notify(`${c.title} accepted — drive out to complete it.`)};
  const complete=()=>{if(!game.active)return;const c=contracts.find(x=>x.id===game.active);const nxp=game.xp+c.xp;const lvl=game.level+Math.floor(nxp/xpNeed);save({...game,active:null,money:game.money+c.pay,xp:nxp%xpNeed,level:Math.max(game.level,lvl),completed:game.completed+1});notify(`Contract complete +${money(c.pay)} · +${c.xp} XP`)};
  const buyTruck=()=>{const cost=18000+game.trucks*9500;if(game.money<cost)return notify('Not enough capital. Complete contracts first.');save({...game,money:game.money-cost,trucks:game.trucks+1});notify('New fleet truck purchased.');};
  const hire=()=>{const cost=4500+game.drivers*1500;if(game.money<cost)return notify('Not enough capital to hire this driver.');save({...game,money:game.money-cost,drivers:game.drivers+1});notify('Driver hired.');};
  const expand=()=>{const cost=12000+game.garage*9000;if(game.money<cost)return notify('Capital required for expansion.');save({...game,money:game.money-cost,garage:game.garage+1});notify('Depot expanded.');};
  return <div className="app"><World game={game} setGame={setGame} activeContract={game.active} setActiveContract={(x)=>save({...game,active:x})}/>
    <header className="topbar"><div className="brand"><div className="brand-mark">TE</div><div><strong>TRUCK EMPIRE</strong><span>LOGISTICS COMMAND</span></div></div><div className="topstats"><div><span>CAPITAL</span><b>{money(game.money)}</b></div><div><span>FLEET</span><b>{game.trucks} TRUCKS</b></div><div><span>RANK</span><b>LVL {game.level}</b></div></div><button className="icon-btn" onClick={()=>{localStorage.removeItem(SAVE_KEY);location.reload()}}>↻</button></header>
    <aside className="leftpanel"><div className="panel-title">COMMAND CENTER</div>{['overview','contracts','garage','drivers'].map(x=><button key={x} className={tab===x?'nav active':'nav'} onClick={()=>setTab(x)}><span>{x==='overview'?'◈':x==='contracts'?'◫':x==='garage'?'▣':'♟'}</span>{x.toUpperCase()}</button>)}<div className="rank-card"><div className="rank-row"><b>LEVEL {game.level}</b><span>{game.xp}/{xpNeed} XP</span></div><div className="xp"><i style={{width:`${xpPct}%`}}/></div><small>Build your fleet and unlock bigger contracts.</small></div></aside>
    <main className="content">
      {tab==='overview'&&<><div className="eyebrow">LIVE OPERATIONS · SECTOR 01</div><h1>Grow the fleet.<br/><em>Own the road.</em></h1><p className="lead">Run freight across a living 3D logistics network. Your company starts small — every delivery builds the next empire.</p><div className="cards"><div className="metric"><span>CONTRACTS COMPLETED</span><b>{game.completed}</b><small>Lifetime deliveries</small></div><div className="metric"><span>DRIVERS</span><b>{game.drivers}</b><small>Active operators</small></div><div className="metric"><span>DEPOT TIER</span><b>{game.garage}</b><small>Expansion level</small></div></div><section className="mission"><div><span className="tag">RECOMMENDED</span><h2>{contracts[0].title}</h2><p>{contracts[0].from} → {contracts[0].to} · {contracts[0].distance} km</p></div><button onClick={()=>takeContract(contracts[0])}>ACCEPT · {money(contracts[0].pay)}</button></section></>}
      {tab==='contracts'&&<><div className="eyebrow">FREIGHT MARKET</div><h1>Available <em>contracts.</em></h1><p className="lead">Pick a haul, accept it, then drive the truck through the 3D world. Completing a contract pays immediately.</p><div className="contract-list">{contracts.map(c=><div className="contract" key={c.id}><div><span className="tag">{c.distance} KM</span><h2>{c.title}</h2><p>{c.from} <b>→</b> {c.to}</p></div><div className="contract-right"><strong>{money(c.pay)}</strong><small>+{c.xp} XP</small><button onClick={()=>takeContract(c)} disabled={game.active===c.id}>{game.active===c.id?'ACTIVE':'ACCEPT'}</button></div></div>)}</div></>}
      {tab==='garage'&&<><div className="eyebrow">FLEET MANAGEMENT</div><h1>Your <em>garage.</em></h1><p className="lead">Maintain a growing fleet and invest profits into faster expansion.</p><div className="garage-grid"><div className="big-stat"><span>OWNED TRUCKS</span><b>{game.trucks}</b><div className="mini-trucks">{Array.from({length:Math.min(game.trucks,6)}).map((_,i)=><span key={i}>▰</span>)}</div></div><div className="action-card"><span>ADD TO FLEET</span><h2>Freight Hauler</h2><p>Reliable starter-class tractor + trailer.</p><button onClick={buyTruck}>BUY · {money(18000+game.trucks*9500)}</button></div><div className="action-card"><span>DEPOT EXPANSION</span><h2>Bay Level {game.garage+1}</h2><p>Add parking capacity and future upgrade slots.</p><button onClick={expand}>EXPAND · {money(12000+game.garage*9000)}</button></div></div></>}
      {tab==='drivers'&&<><div className="eyebrow">HUMAN CAPITAL</div><h1>Build the <em>team.</em></h1><p className="lead">Hire operators to turn your fleet into a scalable logistics business.</p><div className="driver-panel"><div className="driver-count"><span>ACTIVE DRIVERS</span><b>{game.drivers}</b></div><div><h2>Recruit a driver</h2><p>Each hired driver expands your future passive-operations capacity.</p><button onClick={hire}>HIRE · {money(4500+game.drivers*1500)}</button></div></div></>}
    </main>
    {game.active&&<div className="active-job"><span>ACTIVE HAUL</span><b>{contracts.find(c=>c.id===game.active)?.title}</b><small>Drive in the world, then confirm delivery.</small><button onClick={complete}>COMPLETE DELIVERY</button></div>}
    {toast&&<div className="toast">{toast}</div>}
    <div className="reticle">+</div>
  </div>
}

createRoot(document.getElementById('root')).render(<App />);
