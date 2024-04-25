import { ReactNode, useId } from 'react';
import classnames from 'classnames';
import { useTranslation } from 'react-i18next';

type Props = {
    label: string;
    hint?: string;
    element: (id: string) => ReactNode;
    errorMessage?: string;
    className?: string;
};

export default function FormElementGroup(props: Props) {
    const id = useId();
    const { t } = useTranslation();

    return (
        <div className={classnames('govuk-form-group', { 'govuk-form-group--error': props.errorMessage }, props.className)}>
            <label className="govuk-label" htmlFor={id}>
                {props.label}
            </label>
            {props.hint && <span className="govuk-hint">{props.hint}</span>}
            {props.errorMessage && (
                <span className="govuk-error-message">
                    <span className="govuk-visually-hidden">{t('error')}: </span> {props.errorMessage}
                </span>
            )}
            {props.element(id)}
        </div>
    );
}

FormElementGroup.defaultProps = {
    hint: undefined,
    errorMessage: undefined
};
