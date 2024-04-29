import { useTranslation } from 'react-i18next';
import { OptionsOrGroups } from 'react-select';
import Select from 'react-select/async';
import { forwardRef } from 'react';

export type AutocompleteOption<T> = {
    value: T;
    label: string;
};

type Props<T> = {
    id: string;
    value: AutocompleteOption<T> | undefined;
    onChange: (value: any) => void;
    getOptionLabel: (value: any) => string;
    loadOptions: any;
    isLoading: boolean;
    disabled?: boolean;
    options?: OptionsOrGroups<any, any> | boolean;
};

export default forwardRef(function ReactSelectElement<T>(props: Props<T>, ref: any) {
    const { t } = useTranslation();
    const { id, options, value, onChange, loadOptions, isLoading, getOptionLabel, disabled } = props;
    return (
        <Select
            id={id}
            ref={ref}
            isDisabled={disabled}
            styles={{
                control: (provided, state) => {
                    return state.isFocused
                        ? {
                              ...provided,
                              outline: '3px solid #ffdf0f!important',
                              fontFamily: 'Source Sans Pro',
                              borderColor: '#0b0c0c!important',
                              outlineOffset: '0!important',
                              fontSize: '19px',
                              boxShadow: 'inset 0 0 0 2px!important',
                              borderRadius: 0
                          }
                        : {
                              ...provided,
                              border: '2px solid #0b0c0c',
                              fontFamily: 'Source Sans Pro',
                              fontSize: '19px',
                              borderRadius: 0,
                              backgroundColor: 'white',
                              '&:hover': {
                                  borderColor: '#0b0c0c!important'
                              }
                          };
                },
                option: (provided) => {
                    return {
                        ...provided,
                        fontFamily: 'Source Sans Pro',
                        fontSize: '19px',
                        minHeight: '1.2em',
                        padding: '5px',
                        lineHeight: '1.25'
                    };
                },
                singleValue: (provided) => ({ ...provided, color: 'black' })
            }}
            isClearable
            components={{ IndicatorSeparator: () => null }}
            loadingMessage={() => t('searchAutocomplete.loading')}
            noOptionsMessage={() => t('searchAutocomplete.noResults')}
            placeholder={t('searchAutocomplete.placeholder')}
            isLoading={isLoading}
            loadOptions={loadOptions}
            value={value}
            getOptionLabel={getOptionLabel}
            onChange={onChange}
            defaultOptions={options}
        />
    );
});
