import { useEffect, useRef } from "react";
import * as THREE from "three";

/**
 * ThreeCanvas - real WebGL 3D fon. "orbit" sahnasi: aylanayotgan rangli
 * kublar torus ustida +鹦鹉 (parrot) atrofida aylanuvchi so'zlar hissi.
 * three.js (^0.169) bilan. Lightweight: bitta renderer, rAF loop, resize.
 * prefers-reduced-motion da aylanmaydi.
 */

export function ThreeCanvas({ scene }: { scene: "orbit" }) {
  const mountRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const mount = mountRef.current;
    if (!mount) return;

    const reduce = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    const width = mount.clientWidth;
    const height = mount.clientHeight;

    const renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
    renderer.setSize(width, height);
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    mount.appendChild(renderer.domElement);

    const scene3 = new THREE.Scene();
    const camera = new THREE.PerspectiveCamera(55, width / height, 0.1, 100);
    camera.position.set(0, 0, 7);

    const group = new THREE.Group();
    scene3.add(group);

    const geo = new THREE.BoxGeometry(0.18, 0.18, 0.18);
    const colors = [0x58cc02, 0x1cb0f6, 0xbd72ee, 0xffc800];
    const N = 70;
    for (let i = 0; i < N; i++) {
      const mat = new THREE.MeshStandardMaterial({
        color: colors[i % colors.length],
        roughness: 0.4,
        metalness: 0.1,
      });
      const m = new THREE.Mesh(geo, mat);
      const a = (i / N) * Math.PI * 2;
      const r = 3.2 + (i % 4) * 0.22;
      m.position.set(Math.cos(a) * r, Math.sin(a * 2) * 0.7, Math.sin(a) * r);
      group.add(m);
    }

    const light = new THREE.PointLight(0xffffff, 60);
    light.position.set(4, 4, 4);
    scene3.add(light);
    scene3.add(new THREE.AmbientLight(0xffffff, 0.55));

    const onResize = () => {
      const w = mount.clientWidth;
      const h = mount.clientHeight;
      renderer.setSize(w, h);
      camera.aspect = w / h;
      camera.updateProjectionMatrix();
    };
    window.addEventListener("resize", onResize);

    let raf = 0;
    const clock = new THREE.Clock();
    const animate = () => {
      const t = clock.getElapsedTime();
      if (!reduce) {
        group.rotation.y = t * 0.25;
        group.rotation.x = Math.sin(t * 0.2) * 0.15;
      }
      renderer.render(scene3, camera);
      raf = requestAnimationFrame(animate);
    };
    animate();

    return () => {
      cancelAnimationFrame(raf);
      window.removeEventListener("resize", onResize);
      geo.dispose();
      scene3.traverse((o) => {
        if (o instanceof THREE.Mesh) (o.material as THREE.Material).dispose();
      });
      renderer.dispose();
      if (renderer.domElement.parentNode === mount) mount.removeChild(renderer.domElement);
    };
  }, [scene]);

  return <div ref={mountRef} className="absolute inset-0 h-full w-full" />;
}
