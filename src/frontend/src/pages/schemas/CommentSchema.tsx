export const schema = {
    $schema: 'http://json-schema.org/draft-07/schema#',
    title: 'JSON schema for comment',
    type: 'object',
    properties: {
        body: {
            type: 'string'
        }
    },
    required: ['body']
};

export const schemaConfig = {
    errMessages: {
        msg: {
            required: 'Správa komentára je povinná'
        }
    }
};
