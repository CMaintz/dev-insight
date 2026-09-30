import { EffectiveTheme } from '../../core/theme/theme.service';

const CVD_VALIDATED_PALETTE: Record<EffectiveTheme, readonly string[]> = {
  light: ['#2a78d6', '#eb6834', '#1baf7a', '#eda100', '#e87ba4', '#008300', '#4a3aa7', '#e34948'],
  dark: ['#3987e5', '#d95926', '#199e70', '#c98500', '#d55181', '#008300', '#9085e9', '#e66767'],
};

export function seriesColors(theme: EffectiveTheme, count: number): string[] {
  return CVD_VALIDATED_PALETTE[theme].slice(
    0,
    Math.max(1, Math.min(count, CVD_VALIDATED_PALETTE[theme].length)),
  );
}

export function prefersReducedMotion(): boolean {
  return globalThis.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false;
}
