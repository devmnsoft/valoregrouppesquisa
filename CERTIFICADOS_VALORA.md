# Certificados Valora Pulse™

Os certificados HTML, PDF e PNG usam o mesmo view model (`buildCertificateViewModel`) e o mesmo layout lógico (`buildCertificateLayout`).

Precedência do participante: `response.participantName`, `response.participant.name`, `response.respondentName`, `response.identification.name`, usuário participante.

Precedência do emissor: `organization.publicName`, `organization.name`, `organization.legalName`, `survey.issuerName`; placeholders como Empresa Exemplo são substituídos por Valora Group.

A exportação valida nome do participante, pesquisa e emissor antes de gerar arquivos.
