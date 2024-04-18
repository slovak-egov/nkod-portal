import React from 'react';
import Select from 'react-select';

import { CodelistValue } from '../client';

type AutocompleteProps =  {
    id: string;
    options: CodelistValue[];
}

const Autocomplete = (props: AutocompleteProps) => {
    return (
        <Select {...props}  />
    );
};

export default Autocomplete;
