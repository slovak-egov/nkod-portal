import { HTMLAttributes } from 'react';

type Props = {
    type: 'info' | 'warning';
} & HTMLAttributes<HTMLDivElement>;

export default function Alert(props: Props) {
    return (
        <div className={'idsk-warning-text ' + (props.type === 'info' && 'idsk-warning-text--info')} {...props}>
            <div className="govuk-width-container">
                <div className="idsk-warning-text__text">{props.children}</div>
            </div>
        </div>
    );
}

Alert.defaultProps = {
    type: 'info'
};
