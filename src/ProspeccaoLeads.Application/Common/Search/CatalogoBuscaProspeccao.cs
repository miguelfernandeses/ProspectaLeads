namespace ProspeccaoLeads.Application.Common.Search;

public static class CatalogoBuscaProspeccao
{
    public static IReadOnlyList<string> Categorias { get; } =
    [
        "Clínicas odontológicas", "Consultórios médicos", "Clínicas médicas",
        "Psicólogos e terapeutas", "Fisioterapeutas", "Nutricionistas",
        "Farmácias e drogarias", "Clínicas de estética", "Academias de ginástica",
        "Studios de pilates", "Studios de yoga", "Personal trainers", "Salões de beleza",
        "Barbearias", "Estúdios de tatuagem", "Manicure e pedicure",
        "Clínicas de estética e depilação", "Restaurantes", "Pizzarias",
        "Lanchonetes e hamburguerias", "Padarias e confeitarias", "Cafeterias",
        "Churrascarias", "Imobiliárias", "Construtoras e reformas",
        "Engenheiros e arquitetos", "Eletricistas", "Encanadores e hidráulicos",
        "Oficinas mecânicas", "Auto centers", "Autopeças",
        "Lava jatos e estética automotiva", "Despachantes", "Escritórios de contabilidade",
        "Escritórios de advocacia", "Agências de marketing digital",
        "Empresas de tecnologia e TI", "Consultorias financeiras", "Seguradoras e corretoras",
        "Lojas de roupas e moda", "Lojas de calçados", "Óticas", "Livrarias e papelarias",
        "Lojas de eletrônicos", "Supermercados e mercados", "Pet shops",
        "Clínicas veterinárias", "Banho e tosa", "Escolas e colégios",
        "Cursos e treinamentos", "Escolas de idiomas", "Escolas de música",
        "Transportadoras e logística", "Táxi e transporte executivo", "Mudanças e frete"
    ];

    public static IReadOnlyList<string> Cidades { get; } =
    [
        "São Paulo - SP", "Guarulhos - SP", "Osasco - SP", "Santo André - SP",
        "São Bernardo do Campo - SP", "Mogi das Cruzes - SP", "Barueri - SP", "Cotia - SP",
        "Itaquaquecetuba - SP", "Carapicuíba - SP", "Taboão da Serra - SP", "Santos - SP",
        "São Vicente - SP", "Guarujá - SP", "Praia Grande - SP", "Campinas - SP",
        "Araras - SP", "Limeira - SP", "Piracicaba - SP", "Rio Claro - SP", "Leme - SP",
        "Americana - SP", "Santa Bárbara d'Oeste - SP", "Sumaré - SP", "Hortolândia - SP",
        "Jundiaí - SP", "Indaiatuba - SP", "Itu - SP", "Salto - SP", "Valinhos - SP",
        "Vinhedo - SP", "Itatiba - SP", "Bragança Paulista - SP", "Atibaia - SP",
        "Ribeirão Preto - SP", "Franca - SP", "Araraquara - SP", "São Carlos - SP",
        "Barretos - SP", "Jaboticabal - SP", "São José do Rio Preto - SP", "Catanduva - SP",
        "Votuporanga - SP", "Bauru - SP", "Marília - SP", "Jaú - SP", "Lins - SP",
        "Sorocaba - SP", "Botucatu - SP", "Itapetininga - SP", "São José dos Campos - SP",
        "Taubaté - SP", "Jacareí - SP", "Pindamonhangaba - SP", "Presidente Prudente - SP",
        "Araçatuba - SP", "Rio de Janeiro - RJ", "Niterói - RJ", "Nova Iguaçu - RJ",
        "Duque de Caxias - RJ", "São Gonçalo - RJ", "Campos dos Goytacazes - RJ",
        "Petrópolis - RJ", "Nova Friburgo - RJ", "Belo Horizonte - MG", "Contagem - MG",
        "Uberlândia - MG", "Juiz de Fora - MG", "Betim - MG", "Uberaba - MG",
        "Governador Valadares - MG", "Montes Claros - MG", "Curitiba - PR", "Londrina - PR",
        "Maringá - PR", "Foz do Iguaçu - PR", "Ponta Grossa - PR", "Cascavel - PR",
        "Porto Alegre - RS", "Caxias do Sul - RS", "Canoas - RS", "Pelotas - RS",
        "Santa Maria - RS", "Novo Hamburgo - RS", "Florianópolis - SC", "Joinville - SC",
        "Blumenau - SC", "Chapecó - SC", "Criciúma - SC", "Itajaí - SC", "Salvador - BA",
        "Feira de Santana - BA", "Vitória da Conquista - BA", "Camaçari - BA", "Fortaleza - CE",
        "Caucaia - CE", "Juazeiro do Norte - CE", "Recife - PE", "Olinda - PE", "Caruaru - PE",
        "Petrolina - PE", "Brasília - DF", "Manaus - AM", "Belém - PA", "Goiânia - GO",
        "Campo Grande - MS", "Cuiabá - MT", "Natal - RN", "João Pessoa - PB", "Maceió - AL",
        "Aracaju - SE", "Teresina - PI", "São Luís - MA", "Macapá - AP", "Boa Vista - RR",
        "Porto Velho - RO", "Rio Branco - AC", "Palmas - TO", "Vitória - ES"
    ];
}
