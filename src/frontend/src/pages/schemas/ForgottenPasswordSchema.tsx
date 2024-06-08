export const schema = {
    $schema: 'http://json-schema.org/draft-07/schema#',
    title: 'JSON schema for forgotten password',
    type: 'object',
    properties: {
        email: {
            type: 'string',
            format: 'email'
        }
    },
    required: ['email']
};
