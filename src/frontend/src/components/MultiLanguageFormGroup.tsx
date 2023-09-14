import { ReactNode, useId } from "react";
import GridRow from "./GridRow";
import GridColumn from "./GridColumn";
import { Language } from "../client";

interface IProps 
{
    label: string;
    hint?: string;
    languages: Language[]
    element: (id: string, language: Language) => ReactNode;
    errorMessage?: string;
}

export default function MultiLanguageFormGroup(props: IProps)
{
    const id = useId();

    return <div className={'govuk-form-group ' + (props.errorMessage ? 'govuk-form-group--error' : '')}>
        <GridRow>
            <GridColumn widthUnits={3} totalUnits={4}>
                {props.languages.map(lang => <div key={lang.id}>
                    <label className="govuk-label" htmlFor={id}>
                        {props.label} ({lang.name})
                    </label>
                    {props.hint && <span className="govuk-hint">{props.hint}</span>}
                    {props.errorMessage && <span className="govuk-error-message"><span className="govuk-visually-hidden">Chyba: </span> {props.errorMessage}</span>}
                    {props.element(id, lang)}
                </div>)}
            </GridColumn>
            <GridColumn widthUnits={1} totalUnits={4}>
                {/* <div style={{marginTop: '38px'}}>
                <label className="govuk-label" htmlFor={id}>
                    Jazyk
                </label>
                <SelectElement>
                    <option value="sk">SK</option>
                </SelectElement>
                <div style={{verticalAlign: 'middle', display: 'inline-block', marginLeft: '10px'}}>
                    <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path d="M24 10h-10v-10h-4v10h-10v4h10v10h4v-10h10z"/></svg>
                </div>
                </div> */}
            </GridColumn>
        </GridRow>
    </div>
}

MultiLanguageFormGroup.defaultProps = {
    hint: undefined,
    errorMessage: undefined
}