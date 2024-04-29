export const schema = {
    $schema: 'http://json-schema.org/draft-07/schema#',
    title: 'JSON schema for suggestion',
    type: 'object',
    properties: {
        userId: {
            type: 'string',
            format: 'uuid'
        },
        userOrgURI: {
            type: 'string',
            format: 'url'
        },
        userEmail: {
            type: 'string',
            format: 'email'
        },
        orgToUri: {
            type: 'string',
            format: 'url'
        },
        type: {
            type: 'string',
            oneOf: [
                {
                    const: 'PN',
                    title: 'suggestion to the publication of a new dataset/distribution',
                    'title@sk': 'podnet na zverejnenie nového datasetu/distribúcie'
                },
                {
                    const: 'DQ',
                    title: 'suggestion to the data quality of the published dataset/distribution',
                    'title@sk': 'podnet na kvalitu dát zverejneného datasetu/distribúcie'
                },
                {
                    const: 'MQ',
                    title: 'suggestion to the metadata quality of the published dataset/distribution',
                    'title@sk': 'podnet na kvalitu metadát zverejneného datasetu/distribúcie'
                },
                {
                    const: 'O',
                    title: 'other suggestion',
                    'title@sk': 'iný podnet'
                }
            ]
        },
        datasetUri: {
            type: 'string',
            format: 'url'
        },
        title: {
            type: 'string'
        },
        description: {
            type: 'string'
        },
        status: {
            type: 'string',
            oneOf: [
                {
                    const: 'C',
                    title: 'created',
                    'title@sk': 'zaevidovaný',
                    description: 'after suggestion was created',
                    'description@sk': 'po vytvorení podnetu'
                },
                {
                    const: 'P',
                    title: 'in progress',
                    'title@sk': 'v riešení',
                    description: 'after suggestion was assigned to resolver',
                    'description@sk': 'po tom čo sa ujasní, kto bude podnet riešiť a daný poskytovateľ/OVM s tým súhlasí'
                },
                {
                    const: 'R',
                    title: 'resolved',
                    'title@sk': 'vyriešený',
                    description: 'after suggestion was resolved',
                    'description@sk':
                        'po oprave metadát, po oprave datasetu, po zverejnení datasetu alebo ak sa ukáže, že bol podnet bezpredmetný, napr. žiadaný dataset už je zverejnený'
                }
            ]
        }
    },
    required: ['userId', 'orgToUri', 'type', 'title', 'description', 'status', 'userEmail']
};
