-- ==============================================================================
-- SISTEMA DE PROSPECÇÃO DE CLIENTES - SCHEMA DO BANCO DE DADOS SUPABASE
-- ==============================================================================

-- 1. Habilitar extensões necessárias
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- 2. Tabela de Usuários
CREATE TABLE IF NOT EXISTS public.users (
    id UUID PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    email VARCHAR(255) NOT NULL UNIQUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT timezone('utc'::text, now()) NOT NULL
);

-- 3. Tabela de Leads
CREATE TABLE IF NOT EXISTS public.leads (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL,
    name VARCHAR(255) NOT NULL,
    category VARCHAR(150),
    phone VARCHAR(50),
    whatsapp VARCHAR(50),
    email VARCHAR(255),
    address VARCHAR(500),
    city VARCHAR(150),
    state VARCHAR(50),
    cep VARCHAR(20),
    website VARCHAR(500),
    instagram VARCHAR(150),
    rating NUMERIC(3,2),
    reviews_count INT,
    latitude DOUBLE PRECISION,
    longitude DOUBLE PRECISION,
    status VARCHAR(50) NOT NULL DEFAULT 'Novo',
    notes TEXT,
    source VARCHAR(100),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT timezone('utc'::text, now()) NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT timezone('utc'::text, now()) NOT NULL
);

-- 4. Tabela de Histórico de Pesquisas
CREATE TABLE IF NOT EXISTS public.searches (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL,
    niche VARCHAR(200) NOT NULL,
    location VARCHAR(200) NOT NULL,
    result_count INT NOT NULL DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT timezone('utc'::text, now()) NOT NULL
);

-- 5. Índices de Otimização de Busca e Performance
CREATE INDEX IF NOT EXISTS idx_leads_user_id ON public.leads(user_id);
CREATE INDEX IF NOT EXISTS idx_leads_status ON public.leads(user_id, status);
CREATE INDEX IF NOT EXISTS idx_leads_category ON public.leads(user_id, category);
CREATE INDEX IF NOT EXISTS idx_leads_city ON public.leads(user_id, city);
CREATE INDEX IF NOT EXISTS idx_leads_created_at ON public.leads(user_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_searches_user_id ON public.searches(user_id, created_at DESC);

-- ==============================================================================
-- ROW LEVEL SECURITY (RLS) - SEGURANÇA E ISOLAMENTO MULTI-TENANT POR USUÁRIO
-- ==============================================================================

-- Habilitar RLS em todas as tabelas
ALTER TABLE public.users ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.leads ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.searches ENABLE ROW LEVEL SECURITY;

-- Políticas de Acesso para a tabela 'users'
DROP POLICY IF EXISTS "Usuários podem visualizar apenas seu próprio perfil" ON public.users;
CREATE POLICY "Usuários podem visualizar apenas seu próprio perfil"
    ON public.users FOR SELECT
    USING (auth.uid() = id);

DROP POLICY IF EXISTS "Usuários podem atualizar apenas seu próprio perfil" ON public.users;
CREATE POLICY "Usuários podem atualizar apenas seu próprio perfil"
    ON public.users FOR UPDATE
    USING (auth.uid() = id);

-- Políticas de Acesso para a tabela 'leads'
DROP POLICY IF EXISTS "Usuários têm acesso total aos seus próprios leads" ON public.leads;
CREATE POLICY "Usuários têm acesso total aos seus próprios leads"
    ON public.leads FOR ALL
    USING (auth.uid() = user_id)
    WITH CHECK (auth.uid() = user_id);

-- Políticas de Acesso para a tabela 'searches'
DROP POLICY IF EXISTS "Usuários têm acesso total ao seu histórico de pesquisas" ON public.searches;
CREATE POLICY "Usuários têm acesso total ao seu histórico de pesquisas"
    ON public.searches FOR ALL
    USING (auth.uid() = user_id)
    WITH CHECK (auth.uid() = user_id);

-- Trigger para sincronizar novos usuários do Supabase Auth para a tabela public.users
CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS TRIGGER AS $$
BEGIN
    INSERT INTO public.users (id, name, email, created_at)
    VALUES (
        new.id,
        COALESCE(new.raw_user_meta_data->>'name', split_part(new.email, '@', 1)),
        new.email,
        now()
    )
    ON CONFLICT (id) DO NOTHING;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

DROP TRIGGER IF EXISTS on_auth_user_created ON auth.users;
CREATE TRIGGER on_auth_user_created
    AFTER INSERT ON auth.users
    FOR EACH ROW EXECUTE PROCEDURE public.handle_new_user();
