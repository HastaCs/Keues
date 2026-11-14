import { describe, expect, it } from 'vitest';
import { extractQrQueueId } from './qrApi';

describe('extractQrQueueId', () => {
  it('returns the queue id from the first ticket node in an array', () => {
    const flowJson = JSON.stringify([
      { nodeType: 'menu', queueId: null },
      { nodeType: 'ticket', queueId: '11111111-1111-1111-1111-111111111111' },
    ]);

    expect(extractQrQueueId(flowJson)).toBe('11111111-1111-1111-1111-111111111111');
  });

  it('reads menuItems when flowJson is an object', () => {
    const flowJson = JSON.stringify({
      menuItems: [{ nodeType: 'ticket', queueId: 'queue-1' }],
    });

    expect(extractQrQueueId(flowJson)).toBe('queue-1');
  });

  it('returns null when there is no ticket queue', () => {
    expect(extractQrQueueId(JSON.stringify([{ nodeType: 'menu' }]))).toBeNull();
    expect(extractQrQueueId('[]')).toBeNull();
    expect(extractQrQueueId('not-json')).toBeNull();
  });
});
