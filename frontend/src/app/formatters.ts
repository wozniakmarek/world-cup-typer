import { format } from 'date-fns'
import type { MatchPhase, MatchStatus, PredictionSummary, Team } from '../api/types'

export const formatKickoff = (value: string) => format(new Date(value), 'dd.MM HH:mm')

export const formatLongDate = (value: string) => format(new Date(value), 'dd.MM.yyyy HH:mm')

export const toDateTimeLocalValue = (value: string) => {
  const date = new Date(value)
  const offset = date.getTimezoneOffset()
  return new Date(date.getTime() - offset * 60_000).toISOString().slice(0, 16)
}

export const fromDateTimeLocalValue = (value: string) => new Date(value).toISOString()

export const matchStatusLabel: Record<MatchStatus, string> = {
  Scheduled: 'Zaplanowany',
  InProgress: 'Trwa',
  Finished: 'Po 90 min',
  Settled: 'Rozliczony',
  Cancelled: 'Anulowany',
}

export const getPredictionLabel = (prediction?: PredictionSummary | null) =>
  prediction ? `${prediction.predictedHomeScore}:${prediction.predictedAwayScore}` : 'Brak typu'

type MatchContext = {
  phase: MatchPhase
  groupName?: string | null
}

type MatchWithTeams = MatchContext & {
  homeTeam: Team
  awayTeam: Team
}

const phaseLabels: Record<MatchPhase, string> = {
  GroupStage: 'Faza grupowa',
  RoundOf32: '1/16 finału',
  RoundOf16: '1/8 finału',
  QuarterFinal: 'Ćwierćfinał',
  SemiFinal: 'Półfinał',
  ThirdPlace: 'Mecz o 3. miejsce',
  Final: 'Finał',
}

const isoRegionByCountryCode: Record<string, string> = {
  ALG: 'DZ',
  ARG: 'AR',
  AUS: 'AU',
  AUT: 'AT',
  BEL: 'BE',
  BIH: 'BA',
  BRA: 'BR',
  CAN: 'CA',
  CHI: 'CL',
  CIV: 'CI',
  CMR: 'CM',
  COD: 'CD',
  COL: 'CO',
  CPV: 'CV',
  CRC: 'CR',
  CRO: 'HR',
  CUR: 'CW',
  CUW: 'CW',
  CZE: 'CZ',
  DEN: 'DK',
  ECU: 'EC',
  EGY: 'EG',
  ENG: 'GB',
  ESP: 'ES',
  FRA: 'FR',
  GER: 'DE',
  GHA: 'GH',
  HAI: 'HT',
  HON: 'HN',
  IRN: 'IR',
  IRQ: 'IQ',
  ITA: 'IT',
  JPN: 'JP',
  JOR: 'JO',
  KOR: 'KR',
  KSA: 'SA',
  MAR: 'MA',
  MEX: 'MX',
  NED: 'NL',
  NGA: 'NG',
  NOR: 'NO',
  NZL: 'NZ',
  PAN: 'PA',
  PAR: 'PY',
  POL: 'PL',
  POR: 'PT',
  QAT: 'QA',
  RSA: 'ZA',
  SCO: 'GB',
  SEN: 'SN',
  SRB: 'RS',
  SWE: 'SE',
  SUI: 'CH',
  TUN: 'TN',
  TUR: 'TR',
  URU: 'UY',
  URY: 'UY',
  USA: 'US',
  UZB: 'UZ',
  WAL: 'GB',
}

const flagEmojiByCountryCode: Record<string, string> = {
  ENG: '🏴',
  SCO: '🏴',
  WAL: '🏴',
}

const polishTeamNameByName: Record<string, string> = {
  Algeria: 'Algieria',
  Argentina: 'Argentyna',
  Australia: 'Australia',
  Austria: 'Austria',
  Belgium: 'Belgia',
  'Bosnia-Herzegovina': 'Bośnia i Hercegowina',
  Brazil: 'Brazylia',
  Cameroon: 'Kamerun',
  Canada: 'Kanada',
  'Cape Verde Islands': 'Republika Zielonego Przylądka',
  Chile: 'Chile',
  Colombia: 'Kolumbia',
  'Congo DR': 'Demokratyczna Republika Konga',
  'Costa Rica': 'Kostaryka',
  Croatia: 'Chorwacja',
  Curaçao: 'Curaçao',
  Czechia: 'Czechy',
  Denmark: 'Dania',
  Ecuador: 'Ekwador',
  Egypt: 'Egipt',
  England: 'Anglia',
  France: 'Francja',
  Germany: 'Niemcy',
  Ghana: 'Ghana',
  Haiti: 'Haiti',
  Honduras: 'Honduras',
  Iran: 'Iran',
  Iraq: 'Irak',
  Italy: 'Włochy',
  'Ivory Coast': 'Wybrzeże Kości Słoniowej',
  Japan: 'Japonia',
  Jordan: 'Jordania',
  Mexico: 'Meksyk',
  Morocco: 'Maroko',
  Netherlands: 'Holandia',
  'New Zealand': 'Nowa Zelandia',
  Nigeria: 'Nigeria',
  Norway: 'Norwegia',
  Panama: 'Panama',
  Paraguay: 'Paragwaj',
  Poland: 'Polska',
  Portugal: 'Portugalia',
  Qatar: 'Katar',
  'Saudi Arabia': 'Arabia Saudyjska',
  Scotland: 'Szkocja',
  Senegal: 'Senegal',
  Serbia: 'Serbia',
  'South Africa': 'Republika Południowej Afryki',
  'South Korea': 'Korea Południowa',
  Spain: 'Hiszpania',
  Sweden: 'Szwecja',
  Switzerland: 'Szwajcaria',
  Tunisia: 'Tunezja',
  Turkey: 'Turcja',
  Uruguay: 'Urugwaj',
  'United States': 'Stany Zjednoczone',
  Uzbekistan: 'Uzbekistan',
  Wales: 'Walia',
}

const buildRegionalIndicatorFlag = (regionCode: string) => {
  const normalized = regionCode.trim().toUpperCase()
  if (!/^[A-Z]{2}$/.test(normalized)) {
    return null
  }

  return String.fromCodePoint(...[...normalized].map((character) => 0x1f1e6 + character.charCodeAt(0) - 65))
}

export const getTeamFlagEmoji = (team: Team) => {
  const explicitFlagEmoji = team.flagEmoji?.trim()
  if (explicitFlagEmoji && !/^[A-Z]{2,3}$/i.test(explicitFlagEmoji)) {
    return explicitFlagEmoji
  }

  const countryCode = team.countryCode.trim().toUpperCase()
  if (flagEmojiByCountryCode[countryCode]) {
    return flagEmojiByCountryCode[countryCode]
  }

  const regionCode = countryCode.length === 2 ? countryCode : isoRegionByCountryCode[countryCode]
  return regionCode ? buildRegionalIndicatorFlag(regionCode) : null
}

export const translateTeamName = (name: string) => polishTeamNameByName[name.trim()] ?? name

export const formatMatchContext = (match: MatchContext) => {
  const phaseLabel = phaseLabels[match.phase]
  return match.phase === 'GroupStage' && match.groupName
    ? `${phaseLabel} · Grupa ${match.groupName}`
    : phaseLabel
}

export const formatTeamDisplayName = (team: Team) => {
  const flagEmoji = getTeamFlagEmoji(team)
  const teamName = translateTeamName(team.name)
  return flagEmoji ? `${flagEmoji} ${teamName}` : teamName
}

export const hasPlaceholderTeam = (team: Team) => {
  const values = [team.name, team.shortName, team.countryCode].map((value) => value.trim().toUpperCase())

  return values.some((value) =>
    ['UNKNOWN TEAM', 'UNKNOWN', 'TBA', 'TBD', 'TO BE ANNOUNCED'].includes(value)
      || value.startsWith('WINNER GROUP ')
      || value.startsWith('RUNNER-UP GROUP '))
}

export const shouldShowMatchToPlayer = (match: MatchWithTeams) =>
  match.phase === 'GroupStage' || (!hasPlaceholderTeam(match.homeTeam) && !hasPlaceholderTeam(match.awayTeam))

export const getResultBadgeClass = (status: MatchStatus, isSettled: boolean) => {
  if (isSettled || status === 'Settled') {
    return 'bg-emerald-500/15 text-emerald-300 ring-1 ring-inset ring-emerald-500/30'
  }

  if (status === 'Finished' || status === 'InProgress') {
    return 'bg-amber-500/15 text-amber-200 ring-1 ring-inset ring-amber-500/30'
  }

  if (status === 'Cancelled') {
    return 'bg-rose-500/15 text-rose-200 ring-1 ring-inset ring-rose-500/30'
  }

  return 'bg-sky-500/15 text-sky-200 ring-1 ring-inset ring-sky-500/30'
}
