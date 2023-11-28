import { ReactNode, useEffect, useId, useState } from "react";
import GridRow from "./GridRow";
import GridColumn from "./GridColumn";
import { Language, supportedLanguages } from "../client";
import Button from "./Button";
import SelectElementItems from "./SelectElementItems";
import { useTranslation } from "react-i18next";

type Props<T> =
{
    label: string;
    hint?: string;
    values: {[id: string]: T};
    element: (id: string, value: T, onChange: (value: T) => void) => ReactNode;
    onChange: (values: {[id: string]: T}) => void;
    emptyValue: T;
    errorMessage?: {[id: string] : string};
}

export default function MultiLanguageFormGroup<T>(props: Props<T>)
{
    const id = useId();
    const [selectedLanguage, setSelectedLanguage] = useState<Language|null>(null);

    const {t} = useTranslation();

    const values = props.values;

    const existingLanguages = Object.keys(values);
    const newLanguages = supportedLanguages.filter(l => !existingLanguages.includes(l.id));

    useEffect(() => {
        const existingLanguages = Object.keys(values);
        if (selectedLanguage === null || existingLanguages.includes(selectedLanguage.id)) {
            const newLanguages = supportedLanguages.filter(l => !existingLanguages.includes(l.id));
            if (newLanguages.length > 0) {
                setSelectedLanguage(newLanguages[0]);
            } else if (selectedLanguage !== null) {
                setSelectedLanguage(null);
            }
        }
    }, [selectedLanguage, values]);

    const emptyValue = props.emptyValue;
    const onChange = props.onChange;
    useEffect(() => {
        const existingLanguages = Object.keys(values);
        const newValues = {...values};
        let valueChanged = false;
        supportedLanguages.forEach(l => {
            if (l.isRequired) {
                if (!existingLanguages.includes(l.id)) {
                    newValues[l.id] = emptyValue;
                    valueChanged = true;
                }
            }
        });
        if (valueChanged) {
            onChange(newValues);
        }
    }, [values, emptyValue, onChange]);

    return <div className={'govuk-form-group ' + (props.errorMessage && Object.keys(props.errorMessage).length > 0 ? 'govuk-form-group--error' : '')} data-label={props.label}>
        {Object.keys(values).map(lang => {
            const language = supportedLanguages.find(l => l.id === lang);
            if (!language) return null;
            const value = values[lang];

            return <div key={lang}>
                <GridRow>
                    <GridColumn widthUnits={3} totalUnits={4}>
                        <div className="language-input" data-lang={lang}>
                            <label className="govuk-label" htmlFor={id + "_" + lang}>
                                {props.label} ({language.nameInPrimaryLanguage})
                            </label>
                            {props.hint ? <span className="govuk-hint">{props.hint}</span> : null}
                            {props.errorMessage && props.errorMessage[lang] && props.errorMessage[lang] !== '' ? <span className="govuk-error-message"><span className="govuk-visually-hidden">{t('error')}: </span> {props.errorMessage[lang]}</span> : null}
                            {props.element(id + "_" + lang, value, v => props.onChange({...values, [lang]: v}))}
                        </div>
                    </GridColumn>
                    <GridColumn widthUnits={1} totalUnits={4}>
                        {!language.isRequired ? <Button buttonType="secondary" onClick={() => {
                            const newValues = {...values};
                            delete newValues[lang];
                            props.onChange(newValues);
                        }}>
                            {t('removeLanguageVersion')}
                        </Button> : null}
                    </GridColumn>
                </GridRow>
            </div>
        })}
        {newLanguages.length > 0 ? <div className="add-language" style={{marginTop: '10px'}}>
            <SelectElementItems<Language>
                options={newLanguages}
                getValue={l => l ? l.id : ''}
                id={id + '_lang'}
                renderOption={l => l.nameInPrimaryLanguage}
                selectedValue={selectedLanguage ? selectedLanguage.id : ''}
                onChange={lang => props.onChange({...values, [lang]: props.emptyValue})} />
            {selectedLanguage ? <Button buttonType="secondary" style={{marginLeft: '20px'}} onClick={() => props.onChange({...values, [selectedLanguage.id]: props.emptyValue})}>{t('addLanguageVersion')}</Button> : null}
        </div> : null}
    </div>
}

MultiLanguageFormGroup.defaultProps = {
    hint: undefined,
    errorMessage: undefined
}