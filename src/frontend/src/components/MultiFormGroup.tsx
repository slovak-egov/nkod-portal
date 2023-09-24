import { ReactNode, useId } from "react";
import GridRow from "./GridRow";
import GridColumn from "./GridColumn";

type Props =
{
    label: string;
    hint?: string;
    element: (id: string) => ReactNode;
    errorMessage?: string;
}

export default function MultiFormGroup(props: Props)
{
    const id = useId();

    return <div className={'govuk-form-group ' + (props.errorMessage ? 'govuk-form-group--error' : '')}>
        <GridRow>
            <GridColumn widthUnits={3} totalUnits={4}>
                <label className="govuk-label" htmlFor={id}>
                    {props.label}
                </label>
                {props.hint && <span className="govuk-hint">{props.hint}</span>}
                {props.errorMessage && <span className="govuk-error-message"><span className="govuk-visually-hidden">Chyba: </span> {props.errorMessage}</span>}
                {props.element(id)}
            </GridColumn>
            <GridColumn widthUnits={1} totalUnits={4}>
                <div style={{verticalAlign: 'middle', display: 'inline-block',  marginTop: '38px'}}>
                    <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path d="M24 10h-10v-10h-4v10h-10v4h10v10h4v-10h10z"/></svg>
                </div>
            </GridColumn>
        </GridRow>
    </div>
}

MultiFormGroup.defaultProps = {
    hint: undefined,
    errorMessage: undefined
}