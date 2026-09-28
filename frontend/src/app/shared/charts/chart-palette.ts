import { EffectiveTheme } from '../../core/theme/theme.service';

/**
 * Categorical palette (dataviz skill reference instance), validated with validate_palette.js:
 * light — worst adjacent CVD ΔE 9.1, normal-vision ΔE 19.6 (slots 3/4/5 < 3:1 → table view
 * provided for every chart); dark — worst adjacent CVD ΔE 8.4, all slots ≥ 3:1.
 * Slots are assigned in fixed order and follow the entity, never its rank.
 */
export const CATEGORICAL: Record<EffectiveTheme, readonly string[]> = {
  light: ['#2a78d6', '#eb6834', '#1baf7a', '#eda100', '#e87ba4', '#008300', '#4a3aa7', '#e34948'],
  dark: ['#3987e5', '#d95926', '#199e70', '#c98500', '#d55181', '#008300', '#9085e9', '#e66767'],
};

/** Colours for `count` series in slot order. */
export function seriesColors(theme: EffectiveTheme, count: number): string[] {
  return CATEGORICAL[theme].slice(0, Math.max(1, Math.min(count, CATEGORICAL[theme].length)));
}

export function prefersReducedMotion(): boolean {
  return globalThis.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false;
}
