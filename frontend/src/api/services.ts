import { apiClient } from './client'
import type {
  AdminMatch,
  AuthResponse,
  CurrentUser,
  LeaderboardEntry,
  MatchDetails,
  MatchPredictionsResponse,
  MatchSummary,
  MyPrediction,
  Player,
  PredictionSummary,
  RankingProgressPoint,
  RankingProgressSeries,
  ResetPasswordResponse,
  Team,
  NotificationSettings,
  WebPushPublicKey,
} from './types'

export interface LoginPayload {
  login: string
  password: string
}

export interface SavePredictionPayload {
  predictedHomeScore: number
  predictedAwayScore: number
}

export interface UpdateAvatarPayload {
  avatarUrl?: string | null
}

export interface ChangePasswordPayload {
  currentPassword: string
  newPassword: string
}

export interface UpdateNotificationSettingsPayload {
  morningDigestEnabled: boolean
  missingPrediction2hEnabled: boolean
  missingPrediction30mEnabled: boolean
  rankingUpdatedEnabled: boolean
}

export interface SavePushSubscriptionPayload {
  endpoint: string
  keys: {
    p256dh: string
    auth: string
  }
  userAgent?: string
}

export interface RevokePushSubscriptionPayload {
  endpoint: string
}

export interface SavePlayerPayload {
  email: string
  displayName: string
  password?: string
  role: 'Admin' | 'Player'
  isActive?: boolean
}

export interface SaveTeamPayload {
  name: string
  shortName: string
  countryCode: string
  flagEmoji?: string
  groupName?: string
}

export interface SaveMatchPayload {
  externalId?: string
  matchNumber: number
  phase: string
  groupName?: string
  homeTeamId: string
  awayTeamId: string
  homeSlotRule?: string
  awaySlotRule?: string
  kickoffTimeUtc: string
  venue?: string
  status: string
}

export interface SetMatchResultPayload {
  homeScore90: number
  awayScore90: number
  homeScoreFinal?: number | null
  awayScoreFinal?: number | null
  winnerTeamId?: string | null
}

export const authApi = {
  login: async (payload: LoginPayload) => (await apiClient.post<AuthResponse>('/auth/login', payload)).data,
  me: async () => (await apiClient.get<CurrentUser>('/auth/me')).data,
  changePassword: async (payload: ChangePasswordPayload) =>
    (await apiClient.post<CurrentUser>('/auth/change-password', payload)).data,
  updateAvatar: async (payload: UpdateAvatarPayload) =>
    (await apiClient.put<CurrentUser>('/auth/me/avatar', payload)).data,
  logout: async () => apiClient.post('/auth/logout'),
}

export const matchesApi = {
  getAll: async () => (await apiClient.get<MatchSummary[]>('/matches')).data,
  getToday: async () => (await apiClient.get<MatchSummary[]>('/matches/today')).data,
  getUpcoming: async () => (await apiClient.get<MatchSummary[]>('/matches/upcoming')).data,
  getById: async (matchId: string) => (await apiClient.get<MatchDetails>(`/matches/${matchId}`)).data,
  createPrediction: async (matchId: string, payload: SavePredictionPayload) =>
    (await apiClient.post<PredictionSummary>(`/matches/${matchId}/prediction`, payload)).data,
  updatePrediction: async (matchId: string, payload: SavePredictionPayload) =>
    (await apiClient.put<PredictionSummary>(`/matches/${matchId}/prediction`, payload)).data,
  getPredictions: async (matchId: string) =>
    (await apiClient.get<MatchPredictionsResponse>(`/matches/${matchId}/predictions`)).data,
}

export const predictionsApi = {
  getMine: async () => (await apiClient.get<MyPrediction[]>('/predictions/my')).data,
}

export const rankingApi = {
  getAll: async () => (await apiClient.get<LeaderboardEntry[]>('/ranking')).data,
  getTop: async () => (await apiClient.get<LeaderboardEntry[]>('/ranking/top')).data,
  getMine: async () => (await apiClient.get<LeaderboardEntry>('/ranking/me')).data,
  getProgress: async () => (await apiClient.get<RankingProgressPoint[]>('/ranking/progress')).data,
  getProgressForRanking: async () => (await apiClient.get<RankingProgressSeries[]>('/ranking/progress/all')).data,
}

export const notificationsApi = {
  getSettings: async () => (await apiClient.get<NotificationSettings>('/notifications/settings')).data,
  updateSettings: async (payload: UpdateNotificationSettingsPayload) =>
    (await apiClient.put<NotificationSettings>('/notifications/settings', payload)).data,
  getVapidPublicKey: async () => (await apiClient.get<WebPushPublicKey>('/notifications/vapid-public-key')).data,
  saveSubscription: async (payload: SavePushSubscriptionPayload) =>
    apiClient.post('/notifications/subscriptions', payload),
  revokeCurrentSubscription: async (payload: RevokePushSubscriptionPayload) =>
    apiClient.delete('/notifications/subscriptions/current', { data: payload }),
}

export const teamsApi = {
  getAll: async () => (await apiClient.get<Team[]>('/teams')).data,
}

export const adminApi = {
  getPlayers: async () => (await apiClient.get<Player[]>('/admin/players')).data,
  createPlayer: async (payload: SavePlayerPayload) =>
    (await apiClient.post<Player>('/admin/players', payload)).data,
  updatePlayer: async (id: string, payload: Required<Omit<SavePlayerPayload, 'password'>> ) =>
    (await apiClient.put<Player>(`/admin/players/${id}`, payload)).data,
  deactivatePlayer: async (id: string) => apiClient.post(`/admin/players/${id}/deactivate`),
  resetPassword: async (id: string, newPassword?: string) =>
    (await apiClient.post<ResetPasswordResponse>(`/admin/players/${id}/reset-password`, { newPassword })).data,
  createTeam: async (payload: SaveTeamPayload) =>
    (await apiClient.post<Team>('/admin/teams', payload)).data,
  updateTeam: async (id: string, payload: SaveTeamPayload) =>
    (await apiClient.put<Team>(`/admin/teams/${id}`, payload)).data,
  getMatches: async () => (await apiClient.get<AdminMatch[]>('/admin/matches')).data,
  createMatch: async (payload: SaveMatchPayload) =>
    (await apiClient.post<AdminMatch>('/admin/matches', payload)).data,
  updateMatch: async (id: string, payload: SaveMatchPayload) =>
    (await apiClient.put<AdminMatch>(`/admin/matches/${id}`, payload)).data,
  setMatchResult: async (id: string, payload: SetMatchResultPayload) =>
    (await apiClient.put<AdminMatch>(`/admin/matches/${id}/result`, payload)).data,
  settleMatch: async (id: string) => apiClient.post(`/admin/matches/${id}/settle`),
  recalculateRanking: async () => apiClient.post('/admin/matches/recalculate-ranking'),
}
