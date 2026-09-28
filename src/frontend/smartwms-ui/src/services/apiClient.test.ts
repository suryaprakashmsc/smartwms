import { afterEach, describe, expect, it, vi } from 'vitest'
import { fetchAuthSession } from 'aws-amplify/auth'
import { buildApiPath, fetchJson } from './apiClient'

vi.mock('aws-amplify/auth', () => ({ fetchAuthSession: vi.fn() }))
afterEach(() => { vi.restoreAllMocks(); vi.unstubAllGlobals() })

describe('API transport', () => {
  it('encodes filters without dropping false or zero values', () => {
    expect(buildApiPath('products', { searchTerm: 'A&B', page: 0, activeOnly: false, empty: '', absent: null }))
      .toBe('/products?searchTerm=A%26B&page=0&activeOnly=false')
  })
  it('attaches the Cognito access token', async () => {
    vi.mocked(fetchAuthSession).mockResolvedValue({ tokens: { accessToken: { toString: () => 'access-token' } } } as Awaited<ReturnType<typeof fetchAuthSession>>)
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ ok: true })))
    vi.stubGlobal('fetch', fetchMock)
    await expect(fetchJson('/products')).resolves.toEqual({ ok: true })
    expect((fetchMock.mock.calls[0][1].headers as Headers).get('Authorization')).toBe('Bearer access-token')
  })
  it('preserves problem details on a conflict', async () => {
    vi.mocked(fetchAuthSession).mockResolvedValue({})
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ detail: 'Reload the product' }), {
      status: 409, headers: { 'content-type': 'application/problem+json' },
    })))
    await expect(fetchJson('/products')).rejects.toThrow('Reload the product')
  })
  it('handles no-content responses', async () => {
    vi.mocked(fetchAuthSession).mockResolvedValue({})
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 204 })))
    await expect(fetchJson('/products')).resolves.toBeUndefined()
  })
})
