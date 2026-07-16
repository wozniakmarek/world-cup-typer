export type UserRole = 'Admin' | 'Player'

export type MatchStatus = 'Scheduled' | 'InProgress' | 'Finished' | 'Settled' | 'Cancelled'

export type MatchPhase =
  | 'GroupStage'
  | 'RoundOf32'
  | 'RoundOf16'
  | 'QuarterFinal'
  | 'SemiFinal'
  | 'ThirdPlace'
  | 'Final'

export interface CurrentUser {
  id: string
  email: string
  displayName: string
  role: UserRole
  isActive: boolean
  requiresPasswordChange: boolean
  avatarUrl?: string | null
}

export interface AuthResponse {
  token: string
  user: CurrentUser
}

export interface Team {
  id: string
  name: string
  shortName: string
  countryCode: string
  flagEmoji?: string | null
  groupName?: string | null
}

export interface PredictionSummary {
  id: string
  predictedHomeScore: number
  predictedAwayScore: number
  createdAtUtc: string
  updatedAtUtc?: string | null
  lockedAtUtc?: string | null
  points?: number | null
  isExactScore?: boolean | null
  isCorrectOutcome?: boolean | null
}

export interface MatchSummary {
  id: string
  matchNumber: number
  phase: MatchPhase
  groupName?: string | null
  kickoffTimeUtc: string
  venue?: string | null
  status: MatchStatus
  isSettled: boolean
  homeScore90?: number | null
  awayScore90?: number | null
  homeTeam: Team
  awayTeam: Team
  myPrediction?: PredictionSummary | null
  myPoints?: number | null
  canEditPrediction: boolean
}

export interface MatchDetails extends MatchSummary {
  homeScoreFinal?: number | null
  awayScoreFinal?: number | null
  canViewPredictions: boolean
}

export interface AdminMatch {
  id: string
  matchNumber: number
  phase: MatchPhase
  groupName?: string | null
  kickoffTimeUtc: string
  venue?: string | null
  status: MatchStatus
  isSettled: boolean
  predictionsCount: number
  homeScore90?: number | null
  awayScore90?: number | null
  homeTeam: Team
  awayTeam: Team
}

export interface Player {
  id: string
  email: string
  displayName: string
  role: UserRole
  isActive: boolean
  requiresPasswordChange: boolean
  createdAtUtc: string
  lastLoginAtUtc?: string | null
  avatarUrl?: string | null
}

export interface ResetPasswordResponse {
  playerId: string
  temporaryPassword: string
}

export interface MyPrediction {
  matchId: string
  homeTeamName: string
  awayTeamName: string
  kickoffTimeUtc: string
  prediction: PredictionSummary
}

export interface MatchPredictionView {
  predictionId: string
  userId: string
  displayName: string
  predictedHomeScore: number
  predictedAwayScore: number
  points?: number | null
  isExactScore?: boolean | null
  isCorrectOutcome?: boolean | null
}

export interface MatchPredictionsResponse {
  canViewAllPredictions: boolean
  predictions: MatchPredictionView[]
}

export interface LeaderboardEntry {
  position: number
  userId: string
  displayName: string
  totalPoints: number
  exactScoreHits: number
  correctOutcomeHits: number
  predictionsCount: number
  avatarUrl?: string | null
  isCurrentUser: boolean
}

export interface RankingProgressPoint {
  matchId: string
  matchNumber: number
  matchLabel: string
  snapshotAtUtc: string
  totalPoints: number
  exactScoreHits: number
  correctOutcomeHits: number
  predictionsCount: number
  position: number
}

export interface RankingProgressSeries {
  userId: string
  displayName: string
  avatarUrl?: string | null
  isCurrentUser: boolean
  points: RankingProgressPoint[]
}

export interface FinalSummaryStats {
  settledMatchesCount: number
  activePlayersCount: number
  finalLeaderUserId?: string | null
  finalLeaderDisplayName?: string | null
}

export interface FinalRankingPositionPoint {
  matchId: string
  matchNumber: number
  matchLabel: string
  snapshotAtUtc: string
  position: number
  totalPoints: number
}

export interface FinalRankingPositionSeries {
  userId: string
  displayName: string
  avatarUrl?: string | null
  finalPosition: number
  finalPoints: number
  isCurrentUser: boolean
  points: FinalRankingPositionPoint[]
}

export interface FinalRankingEntry {
  userId: string
  displayName: string
  avatarUrl?: string | null
  finalPosition: number
  totalPoints: number
  exactScoreHits: number
  correctOutcomeHits: number
  predictionsCount: number
  isCurrentUser: boolean
}

export interface FinalSummaryFact {
  id: string
  label: string
  title: string
  description: string
  relatedUserIds: string[]
  relatedMatchIds: string[]
}

export interface FinalSummaryResponse {
  stats: FinalSummaryStats
  positionSeries: FinalRankingPositionSeries[]
  finalTop: FinalRankingEntry[]
  globalFacts: FinalSummaryFact[]
}

export interface PersonalFinalSummaryResponse {
  userId: string
  displayName: string
  avatarUrl?: string | null
  finalPosition: number
  totalPoints: number
  exactScoreHits: number
  correctOutcomeHits: number
  predictionsCount: number
  personalFacts: FinalSummaryFact[]
  highlightedMatchIds: string[]
}

export interface NotificationSettings {
  morningDigestEnabled: boolean
  missingPrediction2hEnabled: boolean
  missingPrediction30mEnabled: boolean
  rankingUpdatedEnabled: boolean
  hasActiveSubscription: boolean
}

export interface WebPushPublicKey {
  publicKey: string
}

export interface TestNotificationResult {
  attempted: number
  sent: number
  failed: number
  revoked: number
}
