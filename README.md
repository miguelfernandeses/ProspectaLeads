# ⚡ ProspectAI - Plataforma de Prospecção B2B

> Sistema completo e moderno para busca, gestão, qualificação e prospecção de leads e estabelecimentos comerciais em todo o Brasil.

---

## 🌟 Funcionalidades Principais

- 🔍 **Busca de estabelecimentos reais**:
  - Integração com **OpenStreetMap / Overpass API** com múltiplos espelhos de contingência.
  - Dados não encontrados permanecem vazios; contatos e avaliações não são estimados.
  - Suporte opcional a Apify e Google Places quando suas chaves forem configuradas.
- 🎯 **Gestão e Funil de Leads (CRM)**:
  - Visualização em tabela com busca em tempo real, ordenação dinâmica e filtros por status (*Novo, Contatado, Em Negociação, Fechado, Perdido*).
  - Modal interativo com ações rápidas de contato: **WhatsApp direto**, **Ligação telefônica**, **E-mail**, **Instagram** e rota no **Google Maps**.
  - Registro de anotações internas e histórico de contato por lead.
- 📊 **Dashboard & KPIs em Tempo Real**:
  - Métricas de leads totais, taxa de conversão, leads contatados e distribuição por nicho/cidade.
- 📥 **Exportação de Dados**:
  - Exportação em formato **Excel (.xlsx)** e **CSV** estruturado.
- 🔐 **Autenticação & Segurança**:
  - Login local individual sem serviços pagos ou integração com Supabase Auth.
  - Sessão protegida e páginas privadas com autorização.
  - Suporte a banco de dados **PostgreSQL (Supabase)** ou **SQLite** local para desenvolvimento.

---

## 🏗️ Arquitetura e Tecnologias

- **Frontend & Backend**: [.NET 10](https://dotnet.microsoft.com/) com Blazor Server interativo.
- **Banco de Dados**: Entity Framework Core com PostgreSQL (`Npgsql`) e SQLite.
- **Autenticação**: modo local individual ou Supabase Auth.
- **Design & UI**: CSS customizado com tema Dark moderno, Glassmorphism, badges responsivos e feedback via toasts dinâmicos.

```
src/
 ├── ProspeccaoLeads.Domain/         # Entidades, Enums e Interfaces centrais
 ├── ProspeccaoLeads.Application/    # DTOs, Serviços de Domínio e Casos de Uso
 ├── ProspeccaoLeads.Infrastructure/ # EF Core, Supabase Auth, Overpass OSM, Exportação
 └── ProspeccaoLeads.Web/            # Componentes Blazor, Páginas, Layouts e UI
tests/
 └── ProspeccaoLeads.Tests/          # Bateria de testes unitários e de integração
```

---

## 🚀 Como Executar Localmente

### 1. Pré-requisitos
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) instalado.
- Conta no Supabase não é necessária para o modo local.

### 2. Configurar Variáveis de Ambiente
Copie o arquivo de exemplo:
```bash
cp .env.example .env
```

Para uso individual gratuito, configure suas próprias credenciais no arquivo `.env`:
```env
Authentication__Mode=Local
LocalAuth__Email=seu-email@exemplo.com
LocalAuth__Password=crie-uma-senha-forte
LocalAuth__Name=Administrador
ConnectionStrings__DefaultConnection=
```

Com a conexão vazia, a aplicação usa SQLite local. Para uma futura versão comercial,
remova o modo `Local` e preencha as configurações do Supabase descritas em `.env.example`.

### 3. Rodar a Aplicação
```bash
dotnet build ProspeccaoLeads.slnx
dotnet run --project src/ProspeccaoLeads.Web/ProspeccaoLeads.Web.csproj --urls "http://localhost:5000"
```

Acesse a aplicação no navegador em: **`http://localhost:5000`**

---

## 🧪 Testes Automatizados

Para rodar todos os testes de unidade e integração:
```bash
dotnet test ProspeccaoLeads.slnx
```

---

## 📄 Licença
Distribuído sob a licença MIT.
