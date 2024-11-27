export const schema = {
    $schema: 'http://json-schema.org/draft-07/schema#',
    title: 'JSON schema for application',
    type: 'object',
    properties: {
        userId: {
            type: 'string',
            format: 'uuid'
        },
        title: {
            type: 'string'
        },
        description: {
            type: 'string'
        },
        type: {
            type: 'string',
            oneOf: [
                {
                    const: 'MA',
                    title: 'mobile application',
                    'title@sk': 'mobilná aplikácia'
                },
                {
                    const: 'WA',
                    title: 'web application',
                    'title@sk': 'webová aplikácia'
                },
                {
                    const: 'WP',
                    title: 'web portal',
                    'title@sk': 'webový portál'
                },
                {
                    const: 'V',
                    title: 'visualization',
                    'title@sk': 'vizualizácia'
                },
                {
                    const: 'A',
                    title: 'analysis',
                    'title@sk': 'analýza'
                }
            ]
        },
        theme: {
            type: 'string',
            oneOf: [
                {
                    const: 'ED',
                    title: 'education',
                    'title@sk': 'školstvo'
                },
                {
                    const: 'HE',
                    title: 'health',
                    'title@sk': 'zdravotníctvo'
                },
                {
                    const: 'EN',
                    title: 'environment',
                    'title@sk': 'životné prostredie'
                },
                {
                    const: 'TR',
                    title: 'transport',
                    'title@sk': 'doprava'
                },
                {
                    const: 'CU',
                    title: 'culture',
                    'title@sk': 'kultúra'
                },
                {
                    const: 'TU',
                    title: 'tourism',
                    'title@sk': 'cestovný ruch'
                },
                {
                    const: 'EC',
                    title: 'economy',
                    'title@sk': 'ekonomika'
                },
                {
                    const: 'SO',
                    title: 'social',
                    'title@sk': 'sociálne veci'
                },
                {
                    const: 'PA',
                    title: 'public administration',
                    'title@sk': 'verejná správa'
                },
                {
                    const: 'O',
                    title: 'other',
                    'title@sk': 'ostatné'
                }
            ]
        },
        url: {
            type: 'string',
            format: 'url'
        },
        logo: {
            type: 'string'
        },
        datasetURIs: {
            type: 'array',
            items: {
                type: 'string',
                format: 'url'
            }
        },
        contactName: {
            type: 'string'
        },
        contactSurname: {
            type: 'string'
        },
        contactEmail: {
            type: 'string',
            format: 'email'
        }
    },
    required: ['title', 'description', 'type', 'theme', 'contactName', 'contactSurname', 'contactEmail']
};
