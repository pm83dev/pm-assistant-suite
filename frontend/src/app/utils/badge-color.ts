/**
 * Genera un colore HSL basato su un nome/stringa per i badge
 * Ogni cliente avrà un colore diverso e consistente
 */
export function getBadgeColor(name: string, index?: number): string {
  if (index !== undefined && index >= 0) {
    // Usa l'indice per generare un hue diverso
    const hue = (index * 37) % 360;
    return `hsl(${hue}, 50%, 85%)`;
  }

  // Usa il nome per generare un hue
  let hash = 0;
  for (let i = 0; i < name.length; i++) {
    hash = name.charCodeAt(i) + ((hash << 5) - hash);
  }
  const hue = Math.abs(hash) % 360;
  return `hsl(${hue}, 50%, 85%)`;
}

/**
 * Genera un colore di sfondo HSL per un badge
 */
export function getBadgeBackground(name: string): string {
  let hash = 0;
  for (let i = 0; i < name.length; i++) {
    hash = name.charCodeAt(i) + ((hash << 5) - hash);
  }
  const hue = Math.abs(hash) % 360;
  return `hsl(${hue}, 45%, 30%)`;
}

/**
 * Genera un colore di bordo HSL per un badge
 */
export function getBadgeBorder(name: string): string {
  let hash = 0;
  for (let i = 0; i < name.length; i++) {
    hash = name.charCodeAt(i) + ((hash << 5) - hash);
  }
  const hue = Math.abs(hash) % 360;
  return `hsl(${hue}, 45%, 40%)`;
}
