export const schema = {
    $schema: 'http://json-schema.org/draft-07/schema#',
    title: 'JSON schema for user registration',
    type: 'object',
    properties: {
        firstName: {
            type: 'string'
        },
        lastName: {
            type: 'string'
        },
        email: {
            type: 'string',
            format: 'email'
        },
        password: {
            type: 'string',
            minLength: 6
        },
        passwordConfirm: {
            type: 'string',
            minLength: 6
        }
    },
    required: ['firstName', 'lastName', 'email', 'password', 'passwordConfirm']
};
