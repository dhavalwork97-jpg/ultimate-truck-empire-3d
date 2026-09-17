import * as THREE from 'three';

const M=(color,roughness=.7,metalness=0)=>new THREE.MeshStandardMaterial({color,roughness,metalness});

function box(scene,w,h,d,x,y,z,color,roughness=.75,metalness=0){
  const m=new THREE.Mesh(new THREE.BoxGeometry(w,h,d),M(color,roughness,metalness));
  m.position.set(x,y,z);m.castShadow=true;m.receiveShadow=true;scene.add(m);return m;
}

function tree(scene,x,z,scale=1){
  box(scene,.35*scale,2.2*scale,.35*scale,x,1.1*scale,z,0x4b3425,.95);
  const crown=new THREE.Mesh(new THREE.IcosahedronGeometry(1.7*scale,1),M(0x244b35,.95));
  crown.position.set(x,2.7*scale,z);crown.castShadow=true;scene.add(crown);
  const crown2=new THREE.Mesh(new THREE.IcosahedronGeometry(1.05*scale,1),M(0x326348,.95));
  crown2.position.set(x+.35*scale,3.7*scale,z-.15*scale);crown2.castShadow=true;scene.add(crown2);
}

function streetLight(scene,x,z,rot=0){
  const pole=box(scene,.12,4.6,.12,x,2.3,z,0x46515a,.45,.7);pole.rotation.y=rot;
  const arm=box(scene,1.25,.1,.1,x+.55,4.45,z,0x46515a,.45,.7);arm.rotation.y=rot;
  const lamp=new THREE.Mesh(new THREE.SphereGeometry(.18,12,8),new THREE.MeshStandardMaterial({color:0xffd58a,emissive:0xff9b42,emissiveIntensity:2.5,roughness:.25}));
  lamp.position.set(x+1.05,4.35,z);scene.add(lamp);
}

function roadEdge(scene,x,z,w,d){
  const curb=new THREE.Mesh(new THREE.BoxGeometry(w,.16,d),M(0x6c7378,.8,.15));
  curb.position.set(x,.09,z);curb.receiveShadow=true;scene.add(curb);
}

export function enhanceScene(scene){
  // Cinematic sky dome
  const sky=new THREE.Mesh(
    new THREE.SphereGeometry(235,32,18),
    new THREE.MeshBasicMaterial({color:0x101d31,side:THREE.BackSide})
  );
  scene.add(sky);

  // Distant city skyline, giving the map depth instead of an empty horizon.
  for(let i=0;i<42;i++){
    const x=((i*37)%390)-195;
    const z=((i*61)%330)-165;
    if(Math.abs(x)<18||Math.abs(z)<12)continue;
    const h=8+(i%8)*3.2,w=6+(i%4)*2.5,d=6+(i%3)*2;
    const b=box(scene,w,h,d,x,h/2,z,0x18232d,.9,.05);
    if(i%3===0){
      const roof=new THREE.Mesh(new THREE.BoxGeometry(w*.82,.08,d*.82),M(0x42505a,.7,.3));
      roof.position.set(x,h+.05,z);scene.add(roof);
    }
  }

  // More convincing road shoulders and intersections.
  roadEdge(scene,0,7,220,.22);roadEdge(scene,0,-7,220,.22);
  roadEdge(scene,7,0,.22,220);roadEdge(scene,-7,0,.22,220);
  for(let x=-100;x<=100;x+=20){streetLight(scene,x,-10,0);streetLight(scene,x,10,Math.PI);}
  for(let z=-100;z<=100;z+=20){streetLight(scene,-10,z,Math.PI/2);streetLight(scene,10,z,-Math.PI/2);}

  // More organic roadside greenery.
  for(let i=0;i<58;i++){
    const x=((i*47)%214)-107,z=((i*71)%214)-107;
    if(Math.abs(x)<14||Math.abs(z)<14)continue;
    tree(scene,x,z,.72+(i%4)*.12);
  }

  // Industrial props near the port/iron side.
  for(let i=0;i<10;i++){
    const x=-76+i*4.4,z=45+(i%2)*5;
    box(scene,3.4,2.5,2.6,x,1.25,z,0x59656d,.72,.15);
    box(scene,3.5,.16,2.7,x,2.55,z,0xe85d3f,.65,.2);
  }

  // Warehouse tanks near the depot.
  for(let i=0;i<4;i++){
    const tank=new THREE.Mesh(new THREE.CylinderGeometry(1.4,1.4,4,20),M(0x64717a,.55,.55));
    tank.rotation.z=Math.PI/2;tank.position.set(-20+i*4,2.1,14);tank.castShadow=true;scene.add(tank);
  }

  // Route arrows at major hubs.
  const arrowMat=new THREE.MeshStandardMaterial({color:0xe85d3f,emissive:0x5a1d12,emissiveIntensity:.8,roughness:.5});
  [[0,-72],[62,-72],[-72,52],[72,52],[-70,-48],[70,-48]].forEach(([x,z])=>{
    const a=new THREE.Mesh(new THREE.ConeGeometry(.75,2.4,4),arrowMat);
    a.rotation.x=-Math.PI/2;a.position.set(x,.35,z);a.castShadow=true;scene.add(a);
  });
}

export function enableCinematicLighting(renderer){
  renderer.shadowMap.enabled=true;
  renderer.shadowMap.type=THREE.PCFSoftShadowMap;
}
