import React, { useEffect, useMemo, useRef, useState } from 'react';
import { createRoot } from 'react-dom/client';
import * as THREE from 'three';
import './styles.css';

const SAVE_KEY = 'ute3d-save-v2';
const clamp = (n, a, b) => Math.max(a, Math.min(b, n));
const money = (n) => `$${Math.round(n).toLocaleString('en-US')}`;

const destinations = {
  depot: { id: 'depot', name: 'Company Depot', short: 'DEPOT', pos: new THREE.Vector3(-33, 0, 18) },
  metro: { id: 'metro', name: 'Metro Hub', short: 'METRO HUB', pos: new THREE.Vector3(0, 0, -72) },
  north: { id: 'north', name: 'North Point', short: 'NORTH POINT', pos: new THREE.Vector3(62, 0, -72) },
  port: { id: 'port', name: 'Industrial Port', short: 'PORT', pos: new THREE.Vector3(-72, 0, 52) },
  iron: { id: 'iron', name: 'Ironworks', short: 'IRONWORKS', pos: new THREE.Vector3(72, 0, 52) },
  fresh: { id: 'fresh', name: 'Fresh Foods', short: 'FRESH FOODS', pos: new THREE.Vector3(-70, 0, -48) },
  airport: { id: 'airport', name: 'Airport', short: 'AIRPORT', pos: new THREE.Vector3(70, 0, -48) },
};

const contracts = [
  { id: 'retail', title: 'Retail Restock', cargo: 'Consumer Goods', from: 'metro', to: 'north', pay: 1450, distance: 420, xp: 120 },
  { id: 'steel', title: 'Steel Run', cargo: 'Steel', from: 'port', to: 'iron', pay: 2200, distance: 610, xp: 180 },
  { id: 'cold', title: 'Cold Chain', cargo: 'Refrigerated Food', from: 'fresh', to: 'airport', pay: 3150, distance: 780, xp: 260 },
  { id: 'express', title: 'Express Freight', cargo: 'Priority Freight', from: 'metro', to: 'airport', pay: 4100, distance: 980, xp: 340 },
];

const defaults = {
  money: 25000, level: 1, xp: 0, trucks: 1, drivers: 0, garage: 1,
  completed: 0, active: null, fuel: 100, mileage: 0,
  upgrades: { engine: 0, gearbox: 0, tires: 0, tank: 0 },
};

function loadGame() {
  try {
    const raw = JSON.parse(localStorage.getItem(SAVE_KEY) || localStorage.getItem('ute3d-save-v1'));
    if (!raw) return null;
    return { ...defaults, ...raw, upgrades: { ...defaults.upgrades, ...(raw.upgrades || {}) } };
  } catch { return null; }
}

function mat(color, roughness = .7, metalness = 0) {
  return new THREE.MeshStandardMaterial({ color, roughness, metalness });
}

function Truck({ color = 0xe85d3f, scale = 1, trailer = true }) {
  const group = new THREE.Group();
  const cab = new THREE.Mesh(new THREE.BoxGeometry(2.2 * scale, 1.65 * scale, 3 * scale), mat(color, .72));
  cab.position.set(0, 1.25 * scale, 1.35 * scale); group.add(cab);
  const hood = new THREE.Mesh(new THREE.BoxGeometry(2.2 * scale, .7 * scale, .85 * scale), mat(0x202833, .65));
  hood.position.set(0, .95 * scale, 2.95 * scale); group.add(hood);
  const glass = new THREE.Mesh(new THREE.BoxGeometry(1.7 * scale, .62 * scale, .08 * scale), mat(0x8fc7d8, .2, .1));
  glass.position.set(0, 1.55 * scale, 2.89 * scale); group.add(glass);
  if (trailer) {
    const box = new THREE.Mesh(new THREE.BoxGeometry(2.45 * scale, 2.25 * scale, 4.7 * scale), mat(0xdfe5e8, .8));
    box.position.set(0, 1.48 * scale, -2.1 * scale); group.add(box);
    const stripe = new THREE.Mesh(new THREE.BoxGeometry(2.48 * scale, .18 * scale, 4.72 * scale), mat(color, .7));
    stripe.position.set(0, 1.12 * scale, -2.1 * scale); group.add(stripe);
  }
  const wheelMat = mat(0x11151b, .9);
  const hubMat = mat(0xaeb8c2, .25, .8);
  const positions = trailer ? [[-1.18,.65,2.15],[1.18,.65,2.15],[-1.18,.65,-.6],[1.18,.65,-.6],[-1.18,.65,-3.55],[1.18,.65,-3.55]] : [[-1.18,.65,2],[1.18,.65,2]];
  positions.forEach(([x,y,z]) => {
    const w = new THREE.Mesh(new THREE.CylinderGeometry(.48 * scale,.48 * scale,.32 * scale,20), wheelMat);
    w.rotation.z = Math.PI/2; w.position.set(x*scale,y*scale,z*scale); group.add(w);
    const h = new THREE.Mesh(new THREE.CylinderGeometry(.18*scale,.18*scale,.34*scale,16),hubMat);
    h.rotation.z=Math.PI/2; h.position.copy(w.position); group.add(h);
  });
  return group;
}

function labelSprite(text) {
  const canvas = document.createElement('canvas');
  canvas.width = 512; canvas.height = 128;
  const ctx = canvas.getContext('2d');
  ctx.fillStyle = 'rgba(5,10,16,.86)'; ctx.roundRect(8, 18, 496, 92, 20); ctx.fill();
  ctx.strokeStyle = '#e85d3f'; ctx.lineWidth = 4; ctx.stroke();
  ctx.font = '700 38px Arial'; ctx.textAlign = 'center'; ctx.textBaseline = 'middle'; ctx.fillStyle = '#ffffff'; ctx.fillText(text, 256, 64);
  const texture = new THREE.CanvasTexture(canvas);
  const sprite = new THREE.Sprite(new THREE.SpriteMaterial({ map: texture, transparent: true, depthWrite: false }));
  sprite.scale.set(10, 2.5, 1);
  return sprite;
}

function makeDestination(scene, d, color = 0xe85d3f) {
  const group = new THREE.Group();
  const ring = new THREE.Mesh(new THREE.RingGeometry(4.2, 5.3, 48), new THREE.MeshBasicMaterial({ color, transparent: true, opacity: .78, side: THREE.DoubleSide }));
  ring.rotation.x = -Math.PI / 2; ring.position.y = .08; group.add(ring);
  const inner = new THREE.Mesh(new THREE.CircleGeometry(3.8, 48), new THREE.MeshBasicMaterial({ color, transparent: true, opacity: .09, side: THREE.DoubleSide }));
  inner.rotation.x = -Math.PI / 2; inner.position.y = .075; group.add(inner);
  const beacon = new THREE.Mesh(new THREE.CylinderGeometry(.12, .12, 7, 8), new THREE.MeshBasicMaterial({ color, transparent: true, opacity: .7 }));
  beacon.position.y = 3.5; group.add(beacon);
  const label = labelSprite(d.short); label.position.y = 8; group.add(label);
  group.position.copy(d.pos); scene.add(group);
  return { group, ring };
}

function buildWorld(scene) {
  scene.background = new THREE.Color(0x09111a);
  scene.fog = new THREE.Fog(0x09111a, 65, 190);
  const hemi = new THREE.HemisphereLight(0x9fc7ff, 0x182014, 1.7); scene.add(hemi);
  const sun = new THREE.DirectionalLight(0xffe0b0, 2.5); sun.position.set(-35,45,25); sun.castShadow=true; sun.shadow.mapSize.set(2048,2048); scene.add(sun);

  const ground = new THREE.Mesh(new THREE.PlaneGeometry(240,240), mat(0x17231c,1)); ground.rotation.x=-Math.PI/2; ground.receiveShadow=true; scene.add(ground);
  const roadMat = mat(0x252b31,.92);
  const road = new THREE.Mesh(new THREE.PlaneGeometry(18,220),roadMat); road.rotation.x=-Math.PI/2; road.position.y=.01; scene.add(road);
  const road2 = new THREE.Mesh(new THREE.PlaneGeometry(220,14),roadMat); road2.rotation.x=-Math.PI/2; road2.position.y=.012; scene.add(road2);
  const lineMat = new THREE.MeshBasicMaterial({color:0xf5d76e});
  for(let z=-100;z<110;z+=8){ const l=new THREE.Mesh(new THREE.PlaneGeometry(.25,4),lineMat); l.rotation.x=-Math.PI/2;l.position.set(0,.025,z);scene.add(l); }
  for(let x=-100;x<110;x+=8){ const l=new THREE.Mesh(new THREE.PlaneGeometry(4,.25),lineMat); l.rotation.x=-Math.PI/2;l.position.set(x,.026,0);scene.add(l); }

  const depot = new THREE.Mesh(new THREE.BoxGeometry(34,.25,30),mat(0x303942,.85)); depot.position.set(-33,.15,18); depot.receiveShadow=true;scene.add(depot);
  const building = new THREE.Mesh(new THREE.BoxGeometry(24,7,11),mat(0x48515a,.8)); building.position.set(-33,3.65,10);building.castShadow=true;scene.add(building);
  const roof = new THREE.Mesh(new THREE.BoxGeometry(25,.45,12),mat(0x111820,.5,.3)); roof.position.set(-33,7.35,10);scene.add(roof);
  for(let i=-42;i<=-24;i+=6){ const door=new THREE.Mesh(new THREE.BoxGeometry(4.5,5.2,.12),mat(0x101820,.35,.2));door.position.set(i,2.8,4.45);scene.add(door); }
  const depotSign=labelSprite('COMPANY DEPOT'); depotSign.position.set(-33,10,10); depotSign.scale.set(13,3.2,1); scene.add(depotSign);

  for(let i=0;i<30;i++){ const x=(i*37)%150-75,z=(i*53)%150-75; if(Math.abs(x)<13||Math.abs(z)<11) continue; const h=3+(i%7)*1.4; const b=new THREE.Mesh(new THREE.BoxGeometry(7+(i%3)*3,h,7+(i%4)*2),mat(0x27333d,.9));b.position.set(x,h/2,z);b.castShadow=true;scene.add(b); }
  for(let i=0;i<55;i++){const x=(i*17)%210-105,z=(i*31)%210-105;if(Math.abs(x)<20&&Math.abs(z)<20)continue;const trunk=new THREE.Mesh(new THREE.CylinderGeometry(.18,.24,1.6,8),mat(0x5c402d));trunk.position.set(x,.8,z);scene.add(trunk);const crown=new THREE.Mesh(new THREE.IcosahedronGeometry(1.5+(i%3)*.4,1),mat(0x31563a,1));crown.position.set(x,2.4+(i%2)*.5,z);scene.add(crown);}

  const destinationObjects = {};
  Object.values(destinations).forEach(d => { if(d.id !== 'depot') destinationObjects[d.id] = makeDestination(scene, d); });
  return destinationObjects;
}

function World({ activeContract, onComplete, telemetry }) {
  const mount = useRef(null);
  const gameRef = useRef({ keys:{}, speed:0, angle:0, pos:new THREE.Vector3(-33,0,18), fuel:100, distance:0 });
  const activeRef = useRef(activeContract);
  const completeRef = useRef(onComplete);
  const telemetryRef = useRef(telemetry);
  const truckRef = useRef(null);
  const [ready,setReady] = useState(false);
  useEffect(()=>{activeRef.current=activeContract;},[activeContract]);
  useEffect(()=>{completeRef.current=onComplete;},[onComplete]);
  useEffect(()=>{telemetryRef.current=telemetry;},[telemetry]);

  useEffect(() => {
    const el = mount.current;
    const scene = new THREE.Scene();
    const camera = new THREE.PerspectiveCamera(55, el.clientWidth/el.clientHeight,.1,500);
    const renderer = new THREE.WebGLRenderer({antialias:true,powerPreference:'high-performance'});
    renderer.setPixelRatio(Math.min(window.devicePixelRatio,2)); renderer.setSize(el.clientWidth,el.clientHeight); renderer.shadowMap.enabled=true; renderer.shadowMap.type=THREE.PCFSoftShadowMap; el.appendChild(renderer.domElement);
    buildWorld(scene);
    const truck=Truck({color:0xe85d3f}); truck.position.copy(gameRef.current.pos); truck.traverse(o=>{if(o.isMesh)o.castShadow=true}); scene.add(truck); truckRef.current=truck;
    const parked=[Truck({color:0x3b82f6,scale:.78}),Truck({color:0x9aa3ad,scale:.78}),Truck({color:0x22a06b,scale:.78})];
    parked[0].position.set(-27,.05,18); parked[1].position.set(-35,.05,22); parked[2].position.set(-43,.05,18); parked.forEach(t=>{t.rotation.y=Math.PI/2;t.traverse(o=>{if(o.isMesh)o.castShadow=true});scene.add(t);});
    const keys=gameRef.current.keys;
    const down=e=>{keys[e.code]=true;if(['ArrowUp','ArrowDown','ArrowLeft','ArrowRight','Space'].includes(e.code))e.preventDefault();};
    const up=e=>{keys[e.code]=false;};
    window.addEventListener('keydown',down);window.addEventListener('keyup',up);
    let raf,last=performance.now(),lastTelemetry=0,lastComplete=null;
    const loop=now=>{
      const dt=Math.min((now-last)/1000,.05); last=now; const s=gameRef.current;
      const throttle=(keys.KeyW||keys.ArrowUp)?1:0, brake=(keys.KeyS||keys.ArrowDown)?1:0;
      const steer=(keys.KeyA||keys.ArrowLeft)?-1:((keys.KeyD||keys.ArrowRight)?1:0);
      const traction=1 + (telemetryRef.current?.upgrades?.engine||0)*.06;
      const maxSpeed=18 + (telemetryRef.current?.upgrades?.engine||0)*1.8;
      const rolling=2.2 - (telemetryRef.current?.upgrades?.tires||0)*.12;
      s.speed += (throttle*15*traction-brake*25-s.speed*rolling)*dt;
      if(!throttle && !brake) s.speed *= Math.max(0,1-dt*.8);
      s.speed=clamp(s.speed,-7,maxSpeed);
      const steerStrength=.035*(.7+Math.min(Math.abs(s.speed)/8,1)*.5);
      s.angle += steer*s.speed*steerStrength*dt;
      const dir=new THREE.Vector3(Math.sin(s.angle),0,Math.cos(s.angle));
      const moved=Math.abs(s.speed*dt); s.pos.addScaledVector(dir,s.speed*dt); s.distance += moved;
      s.pos.x=clamp(s.pos.x,-108,108); s.pos.z=clamp(s.pos.z,-108,108);
      if(moved>0) s.fuel=clamp(s.fuel-moved*(.0018+.00035*(Math.max(s.speed,0)/18))*(1-(telemetryRef.current?.upgrades?.tank||0)*.12),0,100);
      if(s.fuel<=0) s.speed=Math.min(s.speed,3);
      if(truckRef.current){truckRef.current.position.copy(s.pos);truckRef.current.rotation.y=s.angle;}
      const active=activeRef.current;
      if(active){
        const c=contracts.find(x=>x.id===active);
        const target=destinations[c?.to]?.pos;
        if(c && target){ const dist=s.pos.distanceTo(target); if(dist<6 && lastComplete!==active){ lastComplete=active; completeRef.current(active); } }
      } else lastComplete=null;
      const target=s.pos.clone(); target.y=0; const camOffset=new THREE.Vector3(-Math.sin(s.angle)*11,7.8,-Math.cos(s.angle)*11); const desired=target.clone().add(camOffset); camera.position.lerp(desired,.09); camera.lookAt(target.clone().add(new THREE.Vector3(0,1.5,0)));
      if(now-lastTelemetry>250){lastTelemetry=now;telemetryRef.current?.report({speed:s.speed,fuel:s.fuel,distance:s.distance,pos:{x:s.pos.x,z:s.pos.z}});}
      renderer.render(scene,camera); raf=requestAnimationFrame(loop);
    };
    raf=requestAnimationFrame(loop); setReady(true);
    const resize=()=>{camera.aspect=el.clientWidth/el.clientHeight;camera.updateProjectionMatrix();renderer.setSize(el.clientWidth,el.clientHeight)}; window.addEventListener('resize',resize);
    return()=>{cancelAnimationFrame(raf);window.removeEventListener('resize',resize);window.removeEventListener('keydown',down);window.removeEventListener('keyup',up);renderer.dispose();if(el.contains(renderer.domElement))el.removeChild(renderer.domElement);};
  },[]);
  return <div className="world" ref={mount}>{!ready&&<div className="loading">INITIALIZING 3D WORLD…</div>}<div className="controls-hint"><b>WASD</b> / <b>ARROWS</b> Drive · <b>SPACE</b> Brake · Enter the glowing destination ring to deliver</div></div>;
}

function App(){
  const saved=useMemo(loadGame,[]);
  const [game,setGame]=useState(saved||defaults);
  const [tab,setTab]=useState('overview'); const [toast,setToast]=useState('');
  const [telemetry,setTelemetry]=useState({speed:0,fuel:game.fuel||100,distance:0,pos:{x:-33,z:18}});
  const xpNeed=100+game.level*80; const xpPct=clamp(game.xp/xpNeed*100,0,100);
  const save=next=>{setGame(next);try{localStorage.setItem(SAVE_KEY,JSON.stringify(next));}catch{}};
  const notify=m=>{setToast(m);setTimeout(()=>setToast(''),2800);};
  const takeContract=c=>{ if(game.active) return notify('Finish the active delivery first.'); save({...game,active:c.id}); notify(`${c.title} accepted — drive to ${destinations[c.to].name}.`); };
  const completeDelivery=id=>{
    if(game.active!==id)return;
    const c=contracts.find(x=>x.id===id); if(!c)return;
    const nxp=game.xp+c.xp; const newLevel=game.level+Math.floor(nxp/xpNeed);
    save({...game,active:null,money:game.money+c.pay,xp:nxp%xpNeed,level:Math.max(game.level,newLevel),completed:game.completed+1,fuel:telemetry.fuel,mileage:game.mileage+telemetry.distance});
    notify(`DELIVERY COMPLETE · ${c.title} · +${money(c.pay)} · +${c.xp} XP`);
  };
  const buyTruck=()=>{const cost=18000+game.trucks*9500;if(game.money<cost)return notify('Not enough capital. Complete contracts first.');save({...game,money:game.money-cost,trucks:game.trucks+1});notify('New fleet truck purchased.');};
  const hire=()=>{const cost=4500+game.drivers*1500;if(game.money<cost)return notify('Not enough capital to hire this driver.');save({...game,money:game.money-cost,drivers:game.drivers+1});notify('Driver hired.');};
  const expand=()=>{const cost=12000+game.garage*9000;if(game.money<cost)return notify('Capital required for expansion.');save({...game,money:game.money-cost,garage:game.garage+1});notify('Depot expanded.');};
  const upgrade=(key)=>{const costs={engine:9000,gearbox:7500,tires:5000,tank:6500};const level=game.upgrades[key]||0;const cost=costs[key]*(level+1);if(game.money<cost)return notify('Not enough capital for this upgrade.');save({...game,money:game.money-cost,upgrades:{...game.upgrades,[key]:level+1}});notify(`${key.toUpperCase()} upgraded to Mk ${level+1}.`);};
  const active=game.active?contracts.find(c=>c.id===game.active):null;
  const report=t=>setTelemetry(v=>({...v,...t}));
  return <div className="app">
    <World activeContract={game.active} onComplete={completeDelivery} telemetry={{...game,report}} />
    <header className="topbar"><div className="brand"><div className="brand-mark">TE</div><div><strong>TRUCK EMPIRE</strong><span>LOGISTICS COMMAND</span></div></div><div className="topstats"><div><span>CAPITAL</span><b>{money(game.money)}</b></div><div><span>FLEET</span><b>{game.trucks} TRUCKS</b></div><div><span>RANK</span><b>LVL {game.level}</b></div></div><button className="icon-btn" title="Reset save" onClick={()=>{localStorage.removeItem(SAVE_KEY);localStorage.removeItem('ute3d-save-v1');location.reload();}}>↻</button></header>
    <aside className="leftpanel"><div className="panel-title">COMMAND CENTER</div>{['overview','contracts','garage','drivers'].map(x=><button key={x} className={tab===x?'nav active':'nav'} onClick={()=>setTab(x)}><span>{x==='overview'?'◈':x==='contracts'?'◫':x==='garage'?'▣':'♟'}</span>{x.toUpperCase()}</button>)}<div className="rank-card"><div className="rank-row"><b>LEVEL {game.level}</b><span>{game.xp}/{xpNeed} XP</span></div><div className="xp"><i style={{width:`${xpPct}%`}}/></div><small>Complete physical deliveries to build the empire.</small></div></aside>
    <main className="content">
      {tab==='overview'&&<>
        <div className="eyebrow">LIVE OPERATIONS · SECTOR 01</div><h1>Grow the fleet.<br/><em>Own the road.</em></h1><p className="lead">A real-time 3D trucking operation. Accept freight, drive the route and enter the destination zone to get paid.</p>
        <div className="cards"><div className="metric"><span>DELIVERIES</span><b>{game.completed}</b><small>Completed contracts</small></div><div className="metric"><span>ODOMETER</span><b>{(game.mileage+telemetry.distance).toFixed(1)} km</b><small>Driving distance</small></div><div className="metric"><span>FUEL</span><b>{Math.round(telemetry.fuel)}%</b><small>Live tank level</small></div></div>
        {active?<div className="active-job"><div><span>ACTIVE HAUL</span><h3>{active.title}</h3><p>{active.cargo} · {destinations[active.from].name} → <strong>{destinations[active.to].name}</strong></p></div><div className="job-meta"><b>{money(active.pay)}</b><small>{active.distance} km route · {Math.round(telemetry.speed*8)} km/h</small></div></div>:<div className="active-job empty"><div><span>NO ACTIVE HAUL</span><h3>Ready for dispatch</h3><p>Open Contracts and accept a job. The destination will glow in the 3D world.</p></div><button onClick={()=>setTab('contracts')}>VIEW CONTRACTS</button></div>}
      </>}
      {tab==='contracts'&&<><div className="eyebrow">DISPATCH BOARD</div><h2>Available Contracts</h2><p className="lead">Accept one haul at a time. Drive to the glowing ring at the destination — delivery completes automatically when your truck enters the zone.</p><div className="contract-list">{contracts.map(c=><div className={`contract ${game.active===c.id?'selected':''}`} key={c.id}><div className="contract-icon">▰</div><div className="contract-main"><b>{c.title}</b><span>{c.cargo}</span><small>{destinations[c.from].short} → {destinations[c.to].short} · {c.distance} km</small></div><div className="contract-pay"><b>{money(c.pay)}</b><span>{c.xp} XP</span></div><button disabled={!!game.active} onClick={()=>takeContract(c)}>{game.active===c.id?'ACTIVE':'ACCEPT'}</button></div>)}</div></>}
      {tab==='garage'&&<><div className="eyebrow">FLEET OPERATIONS</div><h2>Garage & Fleet</h2><p className="lead">Upgrade the active truck. Upgrades affect acceleration, handling, fuel use and top speed in the 3D world.</p><div className="cards"><div className="metric"><span>FLEET SIZE</span><b>{game.trucks}</b><small>Operational trucks</small></div><div className="metric"><span>GARAGE</span><b>LVL {game.garage}</b><small>Depot capacity</small></div></div><div className="upgrade-grid">{[['engine','ENGINE','Acceleration + top speed'],['gearbox','GEARBOX','Power delivery'],['tires','TIRES','Cornering & rolling resistance'],['tank','FUEL TANK','Lower fuel consumption']].map(([k,n,d])=>{const lv=game.upgrades[k]||0;const cost=({engine:9000,gearbox:7500,tires:5000,tank:6500}[k])*(lv+1);return <div className="upgrade" key={k}><span>{n}</span><b>MK {lv}</b><small>{d}</small><button onClick={()=>upgrade(k)}>{money(cost)} · UPGRADE</button></div>})}</div><div className="actions"><button onClick={buyTruck}>BUY TRUCK · {money(18000+game.trucks*9500)}</button><button onClick={expand}>EXPAND DEPOT · {money(12000+game.garage*9000)}</button></div></>}
      {tab==='drivers'&&<><div className="eyebrow">PERSONNEL</div><h2>Driver Division</h2><p className="lead">Build a driver pool for the future automated-fleet layer.</p><div className="cards"><div className="metric"><span>DRIVERS</span><b>{game.drivers}</b><small>Hired operators</small></div><div className="metric"><span>NEXT HIRE</span><b>{money(4500+game.drivers*1500)}</b><small>Recruitment cost</small></div></div><div className="driver-panel"><div><span>DRIVER PROGRAM</span><h3>Owner-operator network</h3><p>Drivers are stored in your company roster now; automated route assignment is the next fleet-system layer.</p></div><button onClick={hire}>HIRE DRIVER</button></div></>}
    </main>
    {active&&<div className="route-banner"><span>ACTIVE ROUTE</span><b>{destinations[active.to].short}</b><small>Drive into the glowing destination ring</small></div>}
    <div className="telemetry"><span>SPD <b>{Math.round(Math.max(0,telemetry.speed)*8)}</b></span><span>FUEL <b>{Math.round(telemetry.fuel)}%</b></span></div>
    {toast&&<div className="toast">{toast}</div>}
  </div>;
}

createRoot(document.getElementById('root')).render(<App />);
