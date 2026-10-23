/** Item da lista de candidatos (resposta enxuta). */
export interface Candidato {
  id: number;
  nomeCompleto: string;
  email: string;
  telefone: string | null;
  areaInteresse: string | null;
  resumoProfissional: string | null;
  dataCadastro: string;
}

export interface Experiencia {
  id: number;
  empresa: string;
  cargo: string;
  /** Data no formato ISO (yyyy-MM-dd). */
  dataInicio: string;
  /** Nulo significa emprego atual. */
  dataFim: string | null;
  descricao: string | null;
}

/** Resposta completa: detalhes, cadastro e edição. */
export interface CandidatoCompleto extends Candidato {
  experiencias: Experiencia[];
  /** Calculado pela API: true quando o usuário logado é o dono do cadastro. */
  podeEditar: boolean;
}

export interface ExperienciaRequest {
  empresa: string;
  cargo: string;
  dataInicio: string;
  dataFim: string | null;
  descricao: string | null;
}

export interface CandidatoRequest {
  nomeCompleto: string;
  email: string;
  telefone: string | null;
  areaInteresse: string | null;
  resumoProfissional: string | null;
  experiencias: ExperienciaRequest[];
}

export interface DadosExtraidos {
  nomeCompleto: string | null;
  email: string | null;
  telefone: string | null;
  areaInteresse: string | null;
  resumoProfissional: string | null;
}
