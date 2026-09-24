import type { MapNodeKind } from './types';

export interface NodeShape {
  clipPath?: string;
  borderRadius?: number | string;
  dashed?: boolean;
  accentLeft?: boolean;
  ring?: number;
  glow?: boolean;
  padding: string;
}

export const NODE_SHAPES: Record<MapNodeKind, NodeShape> = {
  flow: {
    clipPath:
      'polygon(0 0, 100% 0, 100% calc(100% - 12px), calc(50% + 11px) calc(100% - 12px), 50% 100%, calc(50% - 11px) calc(100% - 12px), 0 calc(100% - 12px))',
    padding: '12px 14px 18px',
  },
  menu: {
    clipPath: 'polygon(0 14px, 30px 14px, 40px 0, 100% 0, 100% 100%, 0 100%)',
    padding: '16px 14px 12px',
  },
  ticket: {
    borderRadius: 12,
    dashed: true,
    padding: '12px 14px',
  },
  queue: {
    clipPath:
      'polygon(16px 0, calc(100% - 16px) 0, 100% 50%, calc(100% - 16px) 100%, 16px 100%, 0 50%)',
    padding: '14px 22px',
  },
  counter: {
    clipPath: 'polygon(0 18px, 18px 0, calc(100% - 18px) 0, 100% 18px, 100% 100%, 0 100%)',
    ring: 4,
    glow: true,
    padding: '16px 16px 14px',
  },
  group: {
    borderRadius: 999,
    accentLeft: true,
    padding: '12px 16px',
  },
  user: {
    borderRadius: 999,
    accentLeft: true,
    padding: '10px 16px',
  },
};
