export const schema = {
    $schema: 'http://json-schema.org/draft-07/schema#',
    title: 'JSON schema for user login',
    type: 'object',
    properties: {
        email: {
            type: 'string',
            format: 'email'
        },
        password: {
            type: 'string'
        }
    },
    required: ['email', 'password']
};
