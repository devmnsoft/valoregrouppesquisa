namespace ValoraPesquisa.Domain.Enums;
public static class Roles { public const string AdminValora="admin_valora"; public const string EmpresaAdmin="empresa_admin"; public const string GestorPesquisa="gestor_pesquisa"; public const string Participante="participante"; public static bool IsCompanyRole(string role)=>role is EmpresaAdmin or GestorPesquisa or Participante; }
public static class SurveyStatuses { public const string Draft="draft"; public const string Published="published"; public const string Paused="paused"; public const string Closed="closed"; }
