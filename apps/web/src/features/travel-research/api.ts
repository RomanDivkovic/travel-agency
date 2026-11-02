import { apiClient } from '../../services/apiClient';
import { adminRead, adminMutation } from '../admin/api';
import type { Activity, PageResult } from '../admin/model';
import type {
  AdminProposal,
  CustomerProposal,
  ProposalConfiguration,
  ProposalInput,
  ProposalItem,
  ResearchInput,
  Workspace,
} from './types';
const requestPath = (id: string) =>
  '/travel-requests/' + encodeURIComponent(id);
export const researchApi = {
  workspace: (id: string, page = 1, signal?: AbortSignal) =>
    adminRead<Workspace>(`${requestPath(id)}/research?page=${page}`, signal),
  start: (id: string) =>
    adminMutation<void>(`${requestPath(id)}/research/start`, 'post'),
  notes: (id: string, versionToken: string, notes: string) =>
    adminMutation<void>(`${requestPath(id)}/research`, 'patch', {
      versionToken,
      notes,
    }),
  saveOption: (
    requestId: string,
    input: ResearchInput,
    id?: string,
    versionToken?: string,
  ) =>
    id
      ? adminMutation<void>(
          `${requestPath(requestId)}/research/options/${encodeURIComponent(id)}`,
          'patch',
          { versionToken, option: input },
        )
      : adminMutation<{ id: string }>(
          `${requestPath(requestId)}/research/options`,
          'post',
          input,
        ),
  removeOption: (requestId: string, id: string, versionToken: string) =>
    adminMutation<void>(
      `${requestPath(requestId)}/research/options/${encodeURIComponent(id)}/remove`,
      'post',
      { versionToken },
    ),
  createProposal: (requestId: string) =>
    adminMutation<{ id: string }>(
      `${requestPath(requestId)}/proposals`,
      'post',
    ),
};
export const proposalApi = {
  configuration: (signal?: AbortSignal) =>
    adminRead<ProposalConfiguration>('/proposals/configuration', signal),
  list: (page: number, status: string, search: string, signal?: AbortSignal) =>
    adminRead<PageResult<ProposalItem>>(
      '/proposals?' +
        new URLSearchParams({ page: String(page), status, search }),
      signal,
    ),
  detail: (id: string, signal?: AbortSignal) =>
    adminRead<AdminProposal>('/proposals/' + encodeURIComponent(id), signal),
  save: (id: string, input: ProposalInput) =>
    adminMutation<void>('/proposals/' + encodeURIComponent(id), 'patch', input),
  action: (
    id: string,
    action: 'send' | 'revise' | 'cancel' | 'revoke-access',
    versionToken: string,
  ) =>
    adminMutation<{ id: string } | undefined>(
      `/proposals/${encodeURIComponent(id)}/${action}`,
      'post',
      { versionToken },
    ),
  history: (id: string, page: number, signal?: AbortSignal) =>
    adminRead<PageResult<Activity>>(
      `/proposals/${encodeURIComponent(id)}/activity?page=${page}`,
      signal,
    ),
};
export const publicProposalApi = {
  review: async (token: string, signal?: AbortSignal) =>
    (
      await apiClient.get<CustomerProposal>('/api/public/travel-proposals', {
        headers: { 'X-Proposal-Token': token },
        withCredentials: false,
        signal,
      })
    ).data,
  respond: async (
    token: string,
    proposal: CustomerProposal,
    approve: boolean,
    alternativeId: string | null,
    feedback: string,
  ) =>
    (
      await apiClient.post<CustomerProposal>(
        `/api/public/travel-proposals/${approve ? 'approve' : 'reject'}`,
        {
          version: proposal.version,
          confirmed: true,
          alternativeId,
          termsVersion: proposal.termsVersion,
          feedback,
        },
        { headers: { 'X-Proposal-Token': token }, withCredentials: false },
      )
    ).data,
};
