export interface Credenciais {
  email: string;
  senha: string;
}

export interface Sessao {
  token: string;
  email: string;
  /** Data/hora (UTC, ISO 8601) em que o token expira. */
  expiraEm: string;
}
