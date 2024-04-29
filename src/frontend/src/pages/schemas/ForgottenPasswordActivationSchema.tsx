export const schema = {
    $schema: 'http://json-schema.org/draft-07/schema#',
    title: 'JSON schema for forgotten password activation',
    type: 'object',
    properties: {
        password: {
            type: 'string',
            minLength: 6
        },
        passwordConfirm: {
            type: 'string',
            minLength: 6
        }
    },
    required: ['password', 'passwordConfirm']
};
